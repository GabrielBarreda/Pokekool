using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class CardPopulationRepository : ICardPopulationRepository
{
    private readonly PokekoolDbContext _db;

    public CardPopulationRepository(PokekoolDbContext db) => _db = db;

    public async Task<CardPopulation?> GetByCardIdAndSourceAsync(
        Guid cardId, string source, CancellationToken ct = default)
    {
        var normalized = source.ToLowerInvariant();
        return await _db.CardPopulations
            .FirstOrDefaultAsync(p => p.CardId == cardId && p.Source == normalized, ct);
    }

    public async Task<IReadOnlyList<CardPopulation>> GetByCardIdAsync(
        Guid cardId, CancellationToken ct = default)
        => await _db.CardPopulations
            .Where(p => p.CardId == cardId)
            .ToListAsync(ct);

    public async Task UpsertAsync(CardPopulation population, CancellationToken ct = default)
    {
        var existing = await GetByCardIdAndSourceAsync(population.CardId, population.Source, ct);

        if (existing is null)
        {
            await _db.CardPopulations.AddAsync(population, ct);
        }
        else
        {
            existing.Update(
                population.TotalGraded,
                population.Pop10,
                population.Pop9,
                population.Pop8,
                population.Pop7,
                population.GemRate10);
        }
    }
}
