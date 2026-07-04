using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class CardGradedPriceRepository : ICardGradedPriceRepository
{
    private readonly PokekoolDbContext _db;

    public CardGradedPriceRepository(PokekoolDbContext db) => _db = db;

    public async Task<IReadOnlyList<CardGradedPrice>> GetByCardIdAsync(
        Guid cardId, CancellationToken ct = default)
        => await _db.CardGradedPrices
            .Where(p => p.CardId == cardId)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<CardGradedPrice>>> GetByCardIdsAsync(
        IEnumerable<Guid> cardIds, CancellationToken ct = default)
    {
        var ids    = cardIds.ToHashSet();
        var prices = await _db.CardGradedPrices
            .Where(p => ids.Contains(p.CardId))
            .ToListAsync(ct);

        return prices
            .GroupBy(p => p.CardId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CardGradedPrice>)g.ToList());
    }

    public async Task UpsertBatchAsync(
        IEnumerable<CardGradedPrice> prices, CancellationToken ct = default)
    {
        // Dédupliquer le batch lui-même : si plusieurs variantes (1st Ed Holo, No Symbol Holo…)
        // partagent (cardId, source, company, grade), on garde le prix le plus élevé.
        var list = prices
            .GroupBy(p => (p.CardId, p.Source, p.GradingCompany, p.GradeValue))
            .Select(g => g.MaxBy(p => p.Price) ?? g.First())
            .ToList();
        if (list.Count == 0) return;

        // Charger les entrées existantes pour les cartes concernées en un seul aller-retour
        var cardIds  = list.Select(p => p.CardId).ToHashSet();
        var existing = await _db.CardGradedPrices
            .Where(p => cardIds.Contains(p.CardId))
            .ToListAsync(ct);

        // Clé naturelle : (cardId, source, gradingCompany, gradeValue)
        var lookup = existing.ToDictionary(
            p => (p.CardId, p.Source, p.GradingCompany, p.GradeValue));

        var toInsert = new List<CardGradedPrice>();

        foreach (var price in list)
        {
            var key = (price.CardId, price.Source, price.GradingCompany, price.GradeValue);
            if (lookup.TryGetValue(key, out var found))
                found.UpdatePrice(price.Price);
            else
                toInsert.Add(price);
        }

        if (toInsert.Count > 0)
            await _db.CardGradedPrices.AddRangeAsync(toInsert, ct);
    }
}
