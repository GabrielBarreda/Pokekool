using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Domain.Repositories;

public interface IShopStockRepository
{
    Task<ShopStock?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<ShopStock> Items, int TotalCount)> SearchAsync(
        ShopProductType? type,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(ShopStock stock, CancellationToken ct = default);
    void Remove(ShopStock stock);
}
