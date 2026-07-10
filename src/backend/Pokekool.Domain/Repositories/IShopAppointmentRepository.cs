using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Domain.Repositories;

public interface IShopAppointmentRepository
{
    Task<ShopAppointment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<ShopAppointment> Items, int TotalCount)> SearchAsync(
        AppointmentStatus? status,
        ShopServiceType? serviceType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(ShopAppointment appointment, CancellationToken ct = default);
}
