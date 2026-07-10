using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class ShopStockRepository : IShopStockRepository
{
    private readonly PokekoolDbContext _db;

    public ShopStockRepository(PokekoolDbContext db) => _db = db;

    public Task<ShopStock?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ShopStock.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<(IReadOnlyList<ShopStock> Items, int TotalCount)> SearchAsync(
        ShopProductType? type, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ShopStock.AsQueryable();

        if (type.HasValue) query = query.Where(s => s.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.ProductName != null && s.ProductName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(ShopStock stock, CancellationToken ct = default)
        => await _db.ShopStock.AddAsync(stock, ct);

    public void Remove(ShopStock stock) => _db.ShopStock.Remove(stock);
}
