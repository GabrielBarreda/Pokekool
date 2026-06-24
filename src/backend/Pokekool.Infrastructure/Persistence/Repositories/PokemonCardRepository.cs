using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class PokemonCardRepository : IPokemonCardRepository
{
    private readonly PokekoolDbContext _db;

    public PokemonCardRepository(PokekoolDbContext db) => _db = db;

    public Task<PokemonCard?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PokemonCards
            .Include(c => c.Set)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<PokemonCard?> GetByTcgIdAsync(string tcgId, CancellationToken ct = default)
        => _db.PokemonCards
            .FirstOrDefaultAsync(c => c.TcgId == tcgId, ct);

    public async Task<(IReadOnlyList<PokemonCard> Items, int TotalCount)> SearchAsync(
        string? search,
        Guid? setId,
        string? rarity,
        string? supertype,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.PokemonCards
            .Include(c => c.Set)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        if (setId.HasValue)
            query = query.Where(c => c.SetId == setId.Value);

        if (!string.IsNullOrWhiteSpace(rarity))
            query = query.Where(c => c.Rarity == rarity);

        if (!string.IsNullOrWhiteSpace(supertype))
            query = query.Where(c => c.Supertype == supertype);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(PokemonCard card, CancellationToken ct = default)
        => await _db.PokemonCards.AddAsync(card, ct);

    public async Task AddRangeAsync(IEnumerable<PokemonCard> cards, CancellationToken ct = default)
        => await _db.PokemonCards.AddRangeAsync(cards, ct);

    public async Task<IReadOnlyList<PokemonCard>> GetAllAsync(CancellationToken ct = default)
        => await _db.PokemonCards.OrderBy(c => c.TcgId).ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.PokemonCards.CountAsync(ct);

    public async Task<(IReadOnlyList<string> Rarities, IReadOnlyList<string> Supertypes)> GetDistinctFiltersAsync(
        Guid setId, CancellationToken ct = default)
    {
        var rarities = await _db.PokemonCards
            .Where(c => c.SetId == setId && c.Rarity != null)
            .Select(c => c.Rarity!)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync(ct);

        var supertypes = await _db.PokemonCards
            .Where(c => c.SetId == setId && c.Supertype != null)
            .Select(c => c.Supertype!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);

        return (rarities, supertypes);
    }
}
