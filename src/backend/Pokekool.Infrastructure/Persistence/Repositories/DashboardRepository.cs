using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly PokekoolDbContext _db;

    public DashboardRepository(PokekoolDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecentlyScrapedCardProjection>> GetRecentlyScrapedCardsAsync(
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);

        // Récupère l'Id du prix le plus récent par carte sur les 24 dernières heures
        var latestPriceIds = await _db.CardMarketPrices
            .Where(p => p.FetchedAt >= since)
            .GroupBy(p => p.CardId)
            .Select(g => g.OrderByDescending(p => p.FetchedAt).First().Id)
            .ToListAsync(ct);

        if (latestPriceIds.Count == 0)
            return [];

        var rows = await _db.CardMarketPrices
            .Where(p => latestPriceIds.Contains(p.Id))
            .Join(_db.PokemonCards,
                p => p.CardId,
                c => c.Id,
                (p, c) => new { Price = p, Card = c })
            .Join(_db.PokemonSets,
                x => x.Card.SetId,
                s => s.Id,
                (x, s) => new { x.Price, x.Card, Set = s })
            .OrderByDescending(x => x.Price.FetchedAt)
            .Take(20)
            .Select(x => new RecentlyScrapedCardProjection(
                x.Card.Id.ToString(),
                x.Card.TcgId,
                x.Card.Name,
                x.Card.ImageSmall,
                x.Set.Name,
                x.Card.Rarity,
                x.Price.MarketPrice,
                x.Price.Currency,
                x.Price.Source,
                x.Price.FetchedAt))
            .ToListAsync(ct);

        return rows;
    }

    public async Task<int> GetTotalUsersCollectingAsync(CancellationToken ct = default)
        => await _db.UserCollections
            .Where(uc => uc.IsOwned)
            .Select(uc => uc.UserId)
            .Distinct()
            .CountAsync(ct);
}
