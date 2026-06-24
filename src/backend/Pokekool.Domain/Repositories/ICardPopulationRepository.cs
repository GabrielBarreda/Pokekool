using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface ICardPopulationRepository
{
    Task<CardPopulation?> GetByCardIdAndSourceAsync(Guid cardId, string source, CancellationToken ct = default);
    Task<IReadOnlyList<CardPopulation>> GetByCardIdAsync(Guid cardId, CancellationToken ct = default);
    Task UpsertAsync(CardPopulation population, CancellationToken ct = default);
}
