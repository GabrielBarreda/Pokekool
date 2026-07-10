using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Domain.Repositories;

public interface IShopProductRepository
{
    Task<ShopProduct?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<ShopProduct> Items, int TotalCount)> SearchAsync(
        ShopProductType? type,
        bool? isAvailable,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(ShopProduct product, CancellationToken ct = default);
    void Remove(ShopProduct product);
}
