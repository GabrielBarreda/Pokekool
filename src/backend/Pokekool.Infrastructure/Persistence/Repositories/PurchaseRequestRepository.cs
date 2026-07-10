using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class PurchaseRequestRepository : IPurchaseRequestRepository
{
    private readonly PokekoolDbContext _db;

    public PurchaseRequestRepository(PokekoolDbContext db) => _db = db;

    public async Task AddAsync(PurchaseRequest request, CancellationToken ct = default)
        => await _db.Set<PurchaseRequest>().AddAsync(request, ct);

    public Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<PurchaseRequest>().FirstOrDefaultAsync(pr => pr.Id == id, ct);

    public async Task<(IReadOnlyList<PurchaseRequestProjection> Items, long TotalCount)> GetPagedAsync(
        PurchaseRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Set<PurchaseRequest>().AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(pr => pr.Status == status.Value);

        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(pr => pr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pr => new PurchaseRequestProjection(
                pr.Id,
                pr.ShopProductId,
                pr.ShopProduct != null ? pr.ShopProduct.ProductName : null,
                pr.ShopProduct != null ? pr.ShopProduct.Price : 0m,
                pr.ShopProduct != null ? pr.ShopProduct.Currency : "EUR",
                pr.UserId,
                pr.ClientName,
                pr.ClientEmail,
                pr.Message,
                pr.Status.ToString(),
                pr.CreatedAt,
                pr.UpdatedAt))
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
