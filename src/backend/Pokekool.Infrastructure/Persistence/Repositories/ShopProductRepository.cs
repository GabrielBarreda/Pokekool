using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class ShopProductRepository : IShopProductRepository
{
    private readonly PokekoolDbContext _db;

    public ShopProductRepository(PokekoolDbContext db) => _db = db;

    public Task<ShopProduct?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ShopProducts.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<ShopProduct> Items, int TotalCount)> SearchAsync(
        ShopProductType? type, bool? isAvailable, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ShopProducts.AsQueryable();

        if (type.HasValue) query = query.Where(p => p.Type == type.Value);
        if (isAvailable.HasValue) query = query.Where(p => p.IsAvailable == isAvailable.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.ProductName != null && p.ProductName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(ShopProduct product, CancellationToken ct = default)
        => await _db.ShopProducts.AddAsync(product, ct);

    public void Remove(ShopProduct product) => _db.ShopProducts.Remove(product);
}
