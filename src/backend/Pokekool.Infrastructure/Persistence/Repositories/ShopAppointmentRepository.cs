using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class ShopAppointmentRepository : IShopAppointmentRepository
{
    private readonly PokekoolDbContext _db;

    public ShopAppointmentRepository(PokekoolDbContext db) => _db = db;

    public Task<ShopAppointment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ShopAppointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IReadOnlyList<ShopAppointment> Items, int TotalCount)> SearchAsync(
        AppointmentStatus? status, ShopServiceType? serviceType,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ShopAppointments.AsQueryable();

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        if (serviceType.HasValue) query = query.Where(a => a.ServiceType == serviceType.Value);
        if (from.HasValue) query = query.Where(a => a.ScheduledAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.ScheduledAt < to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(ShopAppointment appointment, CancellationToken ct = default)
        => await _db.ShopAppointments.AddAsync(appointment, ct);
}
