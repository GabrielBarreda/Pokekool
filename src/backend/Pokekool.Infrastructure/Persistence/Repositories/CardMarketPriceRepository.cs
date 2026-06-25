using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class CardMarketPriceRepository : ICardMarketPriceRepository
{
    private readonly PokekoolDbContext _db;

    public CardMarketPriceRepository(PokekoolDbContext db) => _db = db;

    public async Task<IReadOnlyList<CardMarketPrice>> GetByCardIdAsync(Guid cardId, CancellationToken ct = default)
        => await _db.CardMarketPrices
            .Where(p => p.CardId == cardId)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<CardMarketPrice>>> GetByCardIdsAsync(
        IEnumerable<Guid> cardIds, CancellationToken ct = default)
    {
        var ids = cardIds.ToHashSet();
        var prices = await _db.CardMarketPrices
            .Where(p => ids.Contains(p.CardId))
            .ToListAsync(ct);

        return prices
            .GroupBy(p => p.CardId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CardMarketPrice>)g.ToList());
    }

    public async Task UpsertAsync(CardMarketPrice price, CancellationToken ct = default)
    {
        var existing = await _db.CardMarketPrices
            .FirstOrDefaultAsync(p =>
                p.CardId == price.CardId &&
                p.Source == price.Source &&
                p.Variant == price.Variant, ct);

        if (existing is null)
        {
            await _db.CardMarketPrices.AddAsync(price, ct);
        }
        else
        {
            existing.UpdatePrices(price.MarketPrice, price.LowPrice, price.HighPrice);
        }
    }

    public async Task UpsertBatchAsync(IEnumerable<CardMarketPrice> prices, CancellationToken ct = default)
    {
        var priceList = prices.ToList();
        if (priceList.Count == 0) return;

        // Charger toutes les entrées existantes pour les cartes concernées en un seul aller-retour
        var cardIds = priceList.Select(p => p.CardId).ToHashSet();
        var existing = await _db.CardMarketPrices
            .Where(p => cardIds.Contains(p.CardId))
            .ToListAsync(ct);

        var existingLookup = existing.ToDictionary(
            p => (p.CardId, p.Source, p.Variant ?? string.Empty));

        var toInsert = new List<CardMarketPrice>();

        foreach (var price in priceList)
        {
            var key = (price.CardId, price.Source, price.Variant ?? string.Empty);
            if (existingLookup.TryGetValue(key, out var found))
            {
                found.UpdatePrices(
                    price.MarketPrice, price.LowPrice, price.HighPrice,
                    price.Psa7, price.Psa8, price.Psa9, price.Psa95, price.Psa10);
            }
            else
            {
                toInsert.Add(price);
            }
        }

        if (toInsert.Count > 0)
            await _db.CardMarketPrices.AddRangeAsync(toInsert, ct);
    }

    public async Task<IReadOnlyList<Guid>> GetStaleCardIdsAsync(
        string source, int olderThanHours, Guid? setId = null, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-olderThanHours);

        // IDs des cartes qui ont un prix récent pour cette source
        var freshIds = (await _db.CardMarketPrices
            .Where(p => p.Source == source && p.FetchedAt >= cutoff)
            .Select(p => p.CardId)
            .ToListAsync(ct))
            .ToHashSet();

        // Toutes les cartes (filtrées par set si demandé) sauf les fraîches
        var query = _db.PokemonCards.Where(c => !freshIds.Contains(c.Id));
        if (setId.HasValue)
            query = query.Where(c => c.SetId == setId.Value);

        return await query.Select(c => c.Id).ToListAsync(ct);
    }
}
