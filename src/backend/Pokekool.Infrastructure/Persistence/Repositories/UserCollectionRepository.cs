using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class UserCollectionRepository : IUserCollectionRepository
{
    private readonly PokekoolDbContext _db;

    public UserCollectionRepository(PokekoolDbContext db) => _db = db;

    public Task<UserCollection?> GetByUserAndCardAsync(Guid userId, Guid cardId, CancellationToken ct = default)
        => _db.UserCollections
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CardId == cardId, ct);

    public async Task<IReadOnlyList<UserCollection>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserCollections
            .Where(uc => uc.UserId == userId)
            .ToListAsync(ct);

    public async Task AddAsync(UserCollection entry, CancellationToken ct = default)
        => await _db.UserCollections.AddAsync(entry, ct);

    public void Remove(UserCollection entry)
        => _db.UserCollections.Remove(entry);

    public async Task<(IReadOnlyList<CollectionCardProjection> Items, int TotalCount)> GetCardsPagedAsync(
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
        CancellationToken ct = default)
    {
        var query = _db.PokemonCards
            .Include(c => c.Set)
            .AsQueryable();

        if (setId.HasValue)
            query = query.Where(c => c.SetId == setId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{search.Trim()}%") ||
                EF.Functions.ILike(c.TcgId, $"%{search.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(rarity))
            query = query.Where(c => c.Rarity == rarity);

        if (year.HasValue)
            query = query.Where(c => c.Set != null && c.Set.ReleaseDate != null && c.Set.ReleaseDate.Value.Year == year.Value);

        // LEFT JOIN on UserCollection for the current user
        var joinedQuery = query
            .GroupJoin(
                _db.UserCollections.Where(uc => uc.UserId == userId),
                card => card.Id,
                uc => uc.CardId,
                (card, ucGroup) => new { card, ucGroup })
            .SelectMany(
                x => x.ucGroup.DefaultIfEmpty(),
                (x, uc) => new
                {
                    x.card,
                    IsOwned = uc != null && uc.IsOwned,
                    IsFavorite = uc != null && uc.IsFavorite,
                    IsPriority = uc != null && uc.IsPriority,
                });

        if (ownedOnly == true)
            joinedQuery = joinedQuery.Where(x => x.IsOwned);

        if (favoritesOnly == true)
            joinedQuery = joinedQuery.Where(x => x.IsFavorite);

        var totalCount = await joinedQuery.CountAsync(ct);

        joinedQuery = sortBy switch
        {
            "name_desc" => joinedQuery.OrderByDescending(x => x.card.Name),
            "rarity" => joinedQuery.OrderBy(x => x.card.Rarity).ThenBy(x => x.card.Name),
            "number" => joinedQuery.OrderBy(x => x.card.Number),
            _ => joinedQuery.OrderBy(x => x.card.Name),
        };

        var items = await joinedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CollectionCardProjection(
                x.card.Id,
                x.card.TcgId,
                x.card.Number,
                x.card.Name,
                x.card.Supertype,
                x.card.Rarity,
                x.card.Types,
                x.card.ImageSmall,
                x.card.ImageLarge,
                x.card.SetId,
                x.card.Set != null ? x.card.Set.Name : string.Empty,
                x.card.Set != null ? x.card.Set.SymbolUrl : null,
                x.card.Set != null && x.card.Set.ReleaseDate != null ? x.card.Set.ReleaseDate.Value.Year : (int?)null,
                x.IsOwned,
                x.IsFavorite,
                x.IsPriority))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<CollectionStatsProjection> GetStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var totalCards = await _db.PokemonCards.CountAsync(ct);

        var userStats = await _db.UserCollections
            .Where(uc => uc.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                OwnedCards = g.Count(uc => uc.IsOwned),
                FavoriteCards = g.Count(uc => uc.IsFavorite),
                PriorityCards = g.Count(uc => uc.IsPriority),
            })
            .FirstOrDefaultAsync(ct);

        return new CollectionStatsProjection(
            totalCards,
            userStats?.OwnedCards ?? 0,
            userStats?.FavoriteCards ?? 0,
            userStats?.PriorityCards ?? 0);
    }
}
