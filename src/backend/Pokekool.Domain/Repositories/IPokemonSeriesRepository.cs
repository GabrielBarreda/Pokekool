using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface IPokemonSeriesRepository
{
    Task<PokemonSeries?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PokemonSeries?> GetByTcgIdAsync(string tcgId, CancellationToken ct = default);
    Task<IReadOnlyList<PokemonSeries>> ListAsync(CancellationToken ct = default);
    Task AddAsync(PokemonSeries series, CancellationToken ct = default);
}
