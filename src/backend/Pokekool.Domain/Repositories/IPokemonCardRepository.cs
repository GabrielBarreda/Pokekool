using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface IPokemonCardRepository
{
    Task<PokemonCard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PokemonCard?> GetByTcgIdAsync(string tcgId, CancellationToken ct = default);
    Task<(IReadOnlyList<PokemonCard> Items, int TotalCount)> SearchAsync(
        string? search,
        Guid? setId,
        string? rarity,
        string? supertype,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(PokemonCard card, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PokemonCard> cards, CancellationToken ct = default);
    Task<IReadOnlyList<PokemonCard>> GetAllAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetIdsBySetIdAsync(Guid setId, CancellationToken ct = default);

    /// <summary>
    /// Rarités et supertypes distincts présents dans un set.
    /// Utilisé pour griser les filtres indisponibles dans la bourse.
    /// </summary>
    Task<(IReadOnlyList<string> Rarities, IReadOnlyList<string> Supertypes)> GetDistinctFiltersAsync(
        Guid setId, CancellationToken ct = default);
}
