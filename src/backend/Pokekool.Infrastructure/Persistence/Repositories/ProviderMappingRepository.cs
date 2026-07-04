using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class ProviderMappingRepository : IProviderMappingRepository
{
    private readonly PokekoolDbContext _db;

    public ProviderMappingRepository(PokekoolDbContext db) => _db = db;

    // ── Sets ─────────────────────────────────────────────────────────────────

    public async Task<string?> GetSetValueAsync(
        Guid setId, string provider, string key, CancellationToken ct = default)
        => (await _db.ProviderSetMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.SetId    == setId   &&
                    m.Provider == provider &&
                    m.Key      == key, ct))
            ?.Value;

    public async Task<IReadOnlyDictionary<string, string>> GetSetMappingsAsync(
        Guid setId, string provider, CancellationToken ct = default)
    {
        var rows = await _db.ProviderSetMappings
            .AsNoTracking()
            .Where(m => m.SetId == setId && m.Provider == provider)
            .ToListAsync(ct);

        return rows.ToDictionary(m => m.Key, m => m.Value);
    }

    public async Task UpsertSetMappingAsync(
        Guid setId, string provider, string key, string value, CancellationToken ct = default)
    {
        var existing = await _db.ProviderSetMappings
            .FirstOrDefaultAsync(m =>
                m.SetId    == setId   &&
                m.Provider == provider &&
                m.Key      == key, ct);

        if (existing is null)
            await _db.ProviderSetMappings.AddAsync(
                ProviderSetMapping.Create(setId, provider, key, value), ct);
        else
            existing.UpdateValue(value);
    }

    // ── Cartes ───────────────────────────────────────────────────────────────

    public async Task<string?> GetCardValueAsync(
        Guid cardId, string provider, string key, CancellationToken ct = default)
        => (await _db.ProviderCardMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.CardId   == cardId  &&
                    m.Provider == provider &&
                    m.Key      == key, ct))
            ?.Value;

    public async Task<IReadOnlyDictionary<Guid, string>> GetCardValuesBySetAsync(
        Guid setId, string provider, string key, CancellationToken ct = default)
    {
        var rows = await _db.ProviderCardMappings
            .AsNoTracking()
            .Where(m =>
                m.Card!.SetId == setId   &&
                m.Provider    == provider &&
                m.Key         == key)
            .Select(m => new { m.CardId, m.Value })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.CardId, r => r.Value);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetCardValuesByCardIdsAsync(
        IEnumerable<Guid> cardIds, string provider, string key, CancellationToken ct = default)
    {
        var ids = cardIds.ToHashSet();

        var rows = await _db.ProviderCardMappings
            .AsNoTracking()
            .Where(m =>
                ids.Contains(m.CardId) &&
                m.Provider == provider  &&
                m.Key      == key)
            .Select(m => new { m.CardId, m.Value })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.CardId, r => r.Value);
    }

    public async Task UpsertCardMappingAsync(
        Guid cardId, string provider, string key, string value, CancellationToken ct = default)
    {
        var existing = await _db.ProviderCardMappings
            .FirstOrDefaultAsync(m =>
                m.CardId   == cardId  &&
                m.Provider == provider &&
                m.Key      == key, ct);

        if (existing is null)
            await _db.ProviderCardMappings.AddAsync(
                ProviderCardMapping.Create(cardId, provider, key, value), ct);
        else
            existing.UpdateValue(value);
    }
}
