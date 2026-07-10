using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Domain.Repositories;

/// <summary>
/// Accès en lecture/écriture côté client aux entités du module Boutique
/// (ShopProduct, ShopAppointment, Shop). Toute la dépendance concrète aux
/// entités Boutique est isolée dans l'implémentation Infrastructure, seul
/// point d'intégration entre le module Client et le module Boutique.
/// </summary>
public interface IShopClientRepository
{
    /// <summary>Catalogue public : uniquement les produits disponibles (IsAvailable && Quantity > 0).</summary>
    Task<(IReadOnlyList<ShopProductDto> Items, long TotalCount)> GetAvailableProductsPagedAsync(
        string? type,
        decimal? gradeMin,
        decimal? gradeMax,
        decimal? priceMax,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<ShopProductAvailability?> GetProductAvailabilityAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Boutique singleton : id + horaires JSON (pour le calcul des créneaux).</summary>
    Task<ShopHoursInfo?> GetShopHoursAsync(CancellationToken ct = default);

    /// <summary>RDV déjà réservés (non annulés) pour une date donnée.</summary>
    Task<IReadOnlyList<BookedSlot>> GetBookedSlotsAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Crée un RDV client. SEUL endroit qui construit un ShopAppointment (seam Boutique).</summary>
    Task<ShopAppointmentDto> AddAppointmentAsync(NewClientAppointment appointment, CancellationToken ct = default);

    Task<(IReadOnlyList<ShopAppointmentDto> Items, long TotalCount)> GetAppointmentsByEmailPagedAsync(
        string email,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<AppointmentOwnership?> GetAppointmentOwnershipAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>Passe le RDV au statut Cancelled.</summary>
    Task CancelAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
}

public sealed record ShopProductAvailability(
    Guid Id,
    bool IsAvailable,
    int Quantity,
    string? ProductName,
    decimal Price,
    string Currency);

public sealed record ShopHoursInfo(Guid ShopId, string? HoursJson);

public sealed record BookedSlot(DateTimeOffset ScheduledAt, int DurationMinutes);

public sealed record NewClientAppointment(
    Guid ShopId,
    Guid UserId,
    string ClientName,
    string ClientEmail,
    string? ClientPhone,
    string ServiceType,     // enum ShopServiceType name: "PreGrading" | "Restoration" | ...
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string? NotesClient);

public sealed record AppointmentOwnership(
    Guid Id,
    string ClientEmail,
    DateTimeOffset ScheduledAt,
    string Status);
