using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface IPokemonSetRepository
{
    Task<PokemonSet?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PokemonSet?> GetByTcgIdAsync(string tcgId, CancellationToken ct = default);
    Task<IReadOnlyList<PokemonSet>> ListAsync(CancellationToken ct = default);
    Task AddAsync(PokemonSet set, CancellationToken ct = default);
}
