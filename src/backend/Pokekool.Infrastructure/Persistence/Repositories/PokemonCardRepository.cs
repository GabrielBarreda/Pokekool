using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Persistence.Configurations.Pokemon;

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

    public async Task<IReadOnlyList<PokemonCard>> SearchByNameIgnoreCaseAsync(
        string nameQuery, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nameQuery)) return Array.Empty<PokemonCard>();

        var pattern = $"%{nameQuery.Trim()}%";

        return await _db.PokemonCards
            .Include(c => c.Set)
            .Where(c => EF.Functions.ILike(c.Name, pattern))
            // Les noms les plus courts en tête = correspondances les plus précises (un nom qui
            // contient la requête sans grand-chose d'autre autour) — même heuristique que
            // CardTranslationRepository.SearchCardIdsByNameAsync.
            .OrderBy(c => c.Name.Length)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AddAsync(PokemonCard card, CancellationToken ct = default)
        => await _db.PokemonCards.AddAsync(card, ct);

    public async Task AddRangeAsync(IEnumerable<PokemonCard> cards, CancellationToken ct = default)
        => await _db.PokemonCards.AddRangeAsync(cards, ct);

    public async Task<IReadOnlyList<PokemonCard>> GetAllAsync(CancellationToken ct = default)
        => await _db.PokemonCards.OrderBy(c => c.TcgId).ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.PokemonCards.CountAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetIdsBySetIdAsync(Guid setId, CancellationToken ct = default)
        => await _db.PokemonCards
            .Where(c => c.SetId == setId)
            .Select(c => c.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(Guid Id, string TcgId)>> GetIdAndTcgIdBySetIdAsync(Guid setId, CancellationToken ct = default)
    {
        var rows = await _db.PokemonCards
            .Where(c => c.SetId == setId)
            .Select(c => new { c.Id, c.TcgId })
            .ToListAsync(ct);

        return rows.Select(r => (r.Id, r.TcgId)).ToList();
    }

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

    // ── Matching visuel (DINOv2) — comparatif 2026-09-04 ────────────────────────────────────
    // La colonne "embedding" est une shadow property EF (type Pgvector.Vector, voir
    // PokemonCardConfiguration) : absente de l'entité PokemonCard elle-même, donc accédée ici
    // via EF.Property<Vector>(...) plutôt qu'une propriété C# classique. C'est ce qui permet à
    // CosineDistance(...) d'être traduit en SQL (opérateur pgvector "<=>") dans les requêtes
    // ci-dessous — un simple float[] sur l'entité ne le permettrait pas (pas de méthode
    // CosineDistance définie sur float[]).

    public async Task SetEmbeddingAsync(Guid cardId, float[] embedding, CancellationToken ct = default)
    {
        var card = await _db.PokemonCards.FindAsync(new object?[] { cardId }, ct);
        if (card is null)
            return;

        _db.Entry(card)
            .Property<Vector>(PokemonCardEmbeddingColumn.ShadowPropertyName)
            .CurrentValue = new Vector(embedding);
    }

    public async Task<IReadOnlyList<PokemonCard>> GetWithoutEmbeddingAsync(int batchSize, CancellationToken ct = default)
        => await _db.PokemonCards
            .Where(c => EF.Property<Vector?>(c, PokemonCardEmbeddingColumn.ShadowPropertyName) == null)
            .OrderBy(c => c.TcgId)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<int> CountWithoutEmbeddingAsync(CancellationToken ct = default)
        => _db.PokemonCards.CountAsync(
            c => EF.Property<Vector?>(c, PokemonCardEmbeddingColumn.ShadowPropertyName) == null, ct);

    public async Task<IReadOnlyList<(PokemonCard Card, double Distance)>> SearchByEmbeddingAsync(
        float[] embedding, int topN, IReadOnlyList<Guid>? excludedCardIds = null, CancellationToken ct = default)
    {
        var vector = new Vector(embedding);

        var query = _db.PokemonCards
            .Include(c => c.Set)
            .Where(c => EF.Property<Vector?>(c, PokemonCardEmbeddingColumn.ShadowPropertyName) != null);

        if (excludedCardIds is { Count: > 0 })
            query = query.Where(c => !excludedCardIds.Contains(c.Id));

        var results = await query
            .OrderBy(c => EF.Property<Vector>(c, PokemonCardEmbeddingColumn.ShadowPropertyName).CosineDistance(vector))
            .Take(topN)
            .Select(c => new
            {
                Card = c,
                Distance = EF.Property<Vector>(c, PokemonCardEmbeddingColumn.ShadowPropertyName).CosineDistance(vector)
            })
            .ToListAsync(ct);

        return results.Select(r => (r.Card, r.Distance)).ToList();
    }

    public async Task<IReadOnlyList<(PokemonCard Card, double Distance)>> SearchByEmbeddingAmongAsync(
        float[] embedding, IReadOnlyList<Guid> candidateCardIds, int topN, CancellationToken ct = default)
    {
        if (candidateCardIds.Count == 0)
            return Array.Empty<(PokemonCard, double)>();

        var vector = new Vector(embedding);

        var results = await _db.PokemonCards
            .Include(c => c.Set)
            .Where(c => candidateCardIds.Contains(c.Id)
                && EF.Property<Vector?>(c, PokemonCardEmbeddingColumn.ShadowPropertyName) != null)
            .OrderBy(c => EF.Property<Vector>(c, PokemonCardEmbeddingColumn.ShadowPropertyName).CosineDistance(vector))
            .Take(topN)
            .Select(c => new
            {
                Card = c,
                Distance = EF.Property<Vector>(c, PokemonCardEmbeddingColumn.ShadowPropertyName).CosineDistance(vector)
            })
            .ToListAsync(ct);

        return results.Select(r => (r.Card, r.Distance)).ToList();
    }
}
