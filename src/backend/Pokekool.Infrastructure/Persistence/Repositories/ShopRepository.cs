using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Repositories;
using ShopEntity = Pokekool.Domain.Entities.Shop.Shop;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class ShopRepository : IShopRepository
{
    private readonly PokekoolDbContext _db;

    public ShopRepository(PokekoolDbContext db) => _db = db;

    public async Task<ShopEntity> GetOrCreateAsync(CancellationToken ct = default)
    {
        var shop = await _db.Shops.OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        if (shop is not null) return shop;

        shop = ShopEntity.CreateEmpty();
        await _db.Shops.AddAsync(shop, ct);
        await _db.SaveChangesAsync(ct);
        return shop;
    }
}
