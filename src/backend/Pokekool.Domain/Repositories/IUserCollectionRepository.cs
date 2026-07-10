using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface IUserCollectionRepository
{
    Task<UserCollection?> GetByUserAndCardAsync(Guid userId, Guid cardId, CancellationToken ct = default);
    Task<IReadOnlyList<UserCollection>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserCollection entry, CancellationToken ct = default);
    void Remove(UserCollection entry);

    /// <summary>
    /// Paged list of cards with collection status for the given user.
    /// </summary>
    Task<(IReadOnlyList<CollectionCardProjection> Items, int TotalCount)> GetCardsPagedAsync(
        Guid? userId,
        Guid? setId,
        string? search,
        string? rarity,
        int? year,
        bool? ownedOnly,
        bool? favoritesOnly,
        string? sortBy,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<CollectionStatsProjection> GetStatsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Valeur estimée de la collection : somme du meilleur prix marché par carte possédée.
    /// </summary>
    Task<(decimal TotalValue, int CardCount)> GetCollectionValueAsync(Guid userId, CancellationToken ct = default);
}

public sealed record CollectionCardProjection(
    Guid Id,
    string TcgId,
    string Number,
    string Name,
    string? Supertype,
    string? Rarity,
    string[]? Types,
    string? ImageSmall,
    string? ImageLarge,
    Guid SetId,
    string SetName,
    string? SetSymbolUrl,
    int? ReleaseYear,
    bool IsOwned,
    bool IsFavorite,
    bool IsPriority
);

public sealed record CollectionStatsProjection(
    int TotalCards,
    int OwnedCards,
    int FavoriteCards,
    int PriorityCards
);
