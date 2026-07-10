using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Infrastructure.Persistence.Repositories;

/// <summary>
/// SEAM D'INTÉGRATION module Client ↔ module Boutique.
/// Toutes les hypothèses sur la forme concrète des entités Boutique
/// (Shop, ShopProduct, ShopAppointment) sont concentrées ici. Si le module
/// Boutique expose des noms/factories différents, seul ce fichier est à ajuster.
///
/// Hypothèses (à confirmer avec l'agent Boutique) :
///  - Shop.Hours : string? (JSON des horaires). Sinon renommer ci-dessous.
///  - ShopProduct : Type(ShopProductType), CardId?, ProductName?, Condition?, GradingCompany?,
///    GradeValue?, Price, Currency, Quantity, IsAvailable, Description?, PhotoUrl?.
///  - ShopAppointment : factory CreateForClient(...) ci-dessous, méthode ChangeStatus(AppointmentStatus).
///    Le RDV n'a PAS de UserId : l'appartenance client est déterminée par ClientEmail.
/// </summary>
internal sealed class ShopClientRepository : IShopClientRepository
{
    private const int DefaultDurationMinutes = 60;

    private readonly PokekoolDbContext _db;

    public ShopClientRepository(PokekoolDbContext db) => _db = db;

    // ── Catalogue ──────────────────────────────────────────────
    public async Task<(IReadOnlyList<ShopProductDto> Items, long TotalCount)> GetAvailableProductsPagedAsync(
        string? type, decimal? gradeMin, decimal? gradeMax, decimal? priceMax, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Set<ShopProduct>()
            .AsNoTracking()
            .Where(p => p.IsAvailable && p.Quantity > 0);

        if (!string.IsNullOrWhiteSpace(type)
            && Enum.TryParse<ShopProductType>(type, ignoreCase: true, out var parsedType))
        {
            query = query.Where(p => p.Type == parsedType);
        }

        if (gradeMin.HasValue) query = query.Where(p => p.GradeValue >= gradeMin.Value);
        if (gradeMax.HasValue) query = query.Where(p => p.GradeValue <= gradeMax.Value);
        if (priceMax.HasValue) query = query.Where(p => p.Price <= priceMax.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p => p.ProductName != null && EF.Functions.ILike(p.ProductName, term));
        }

        var total = await query.LongCountAsync(ct);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Enrichissement nom/image carte pour les produits liés à une carte du référentiel.
        var cardIds = products.Where(p => p.CardId.HasValue).Select(p => p.CardId!.Value).Distinct().ToList();
        var cards = cardIds.Count == 0
            ? new Dictionary<Guid, (string Name, string? Image)>()
            : await _db.PokemonCards
                .Where(c => cardIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.ImageSmall })
                .ToDictionaryAsync(c => c.Id, c => (Name: c.Name, Image: c.ImageSmall), ct);

        var items = products.Select(p =>
        {
            string? cardName = null, cardImage = null;
            if (p.CardId.HasValue && cards.TryGetValue(p.CardId.Value, out var c))
            {
                cardName = c.Name;
                cardImage = c.Image;
            }
            return new ShopProductDto(
                p.Id, p.ShopId, p.Type.ToString(), p.CardId, cardName, cardImage,
                p.ProductName, p.Condition, p.GradingCompany, p.GradeValue,
                p.Price, p.Currency, p.Quantity, p.IsAvailable, p.Description, p.PhotoUrl,
                p.CreatedAt, p.UpdatedAt);
        }).ToList();

        return (items, total);
    }

    public async Task<ShopProductAvailability?> GetProductAvailabilityAsync(Guid productId, CancellationToken ct = default)
    {
        return await _db.Set<ShopProduct>()
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ShopProductAvailability(p.Id, p.IsAvailable, p.Quantity, p.ProductName, p.Price, p.Currency))
            .FirstOrDefaultAsync(ct);
    }

    // ── Boutique / horaires ────────────────────────────────────
    public async Task<ShopHoursInfo?> GetShopHoursAsync(CancellationToken ct = default)
    {
        return await _db.Set<Pokekool.Domain.Entities.Shop.Shop>()
            .AsNoTracking()
            .Select(s => new ShopHoursInfo(s.Id, s.HoursJson))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<BookedSlot>> GetBookedSlotsAsync(DateOnly date, CancellationToken ct = default)
    {
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);

        return await _db.Set<ShopAppointment>()
            .AsNoTracking()
            .Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status != AppointmentStatus.Cancelled)
            .Select(a => new BookedSlot(a.ScheduledAt, a.DurationMinutes))
            .ToListAsync(ct);
    }

    // ── RDV ────────────────────────────────────────────────────
    public async Task<ShopAppointmentDto> AddAppointmentAsync(NewClientAppointment appointment, CancellationToken ct = default)
    {
        var serviceType = Enum.TryParse<ShopServiceType>(appointment.ServiceType, ignoreCase: true, out var st)
            ? st
            : ShopServiceType.Other;

        // Factory réelle du module Boutique (ShopAppointment.Create). Statut initial = Pending.
        var entity = ShopAppointment.Create(
            appointment.ShopId,
            appointment.ClientName,
            appointment.ClientEmail,
            appointment.ClientPhone,
            serviceType,
            appointment.ScheduledAt,
            appointment.DurationMinutes <= 0 ? DefaultDurationMinutes : appointment.DurationMinutes,
            AppointmentStatus.Pending,
            appointment.NotesClient,
            notesInternal: null,
            assignedToUserId: null);

        await _db.Set<ShopAppointment>().AddAsync(entity, ct);

        return Map(entity);
    }

    public async Task<(IReadOnlyList<ShopAppointmentDto> Items, long TotalCount)> GetAppointmentsByEmailPagedAsync(
        string email, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Set<ShopAppointment>()
            .AsNoTracking()
            .Where(a => a.ClientEmail == email);

        var total = await query.LongCountAsync(ct);

        var entities = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (entities.Select(Map).ToList(), total);
    }

    public async Task<AppointmentOwnership?> GetAppointmentOwnershipAsync(Guid appointmentId, CancellationToken ct = default)
    {
        return await _db.Set<ShopAppointment>()
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(a => new AppointmentOwnership(a.Id, a.ClientEmail, a.ScheduledAt, a.Status.ToString()))
            .FirstOrDefaultAsync(ct);
    }

    public async Task CancelAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var entity = await _db.Set<ShopAppointment>().FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
        if (entity is null) return;

        // >>> SEAM : méthode de changement de statut de l'entité Boutique. <<<
        entity.ChangeStatus(AppointmentStatus.Cancelled);
    }

    private static ShopAppointmentDto Map(ShopAppointment a) => new(
        a.Id, a.ShopId, a.ClientName, a.ClientEmail, a.ClientPhone,
        a.ServiceType.ToString(), a.ScheduledAt, a.DurationMinutes, a.Status.ToString(),
        a.NotesClient, a.NotesInternal, a.AssignedToUserId, a.CreatedAt, a.UpdatedAt);
}
