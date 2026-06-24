using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class ProviderSettingRepository : IProviderSettingRepository
{
    private readonly PokekoolDbContext _db;

    public ProviderSettingRepository(PokekoolDbContext db) => _db = db;

    public async Task<ProviderSetting?> GetAsync(string provider, string key, CancellationToken ct = default)
    {
        // Les valeurs sont toujours stockées en lowercase (ProviderSetting.Create normalise)
        // On normalise ici aussi pour la comparaison PK exacte (pas de ToLower en EF LINQ)
        var p = provider.ToLowerInvariant();
        var k = key.ToLowerInvariant();
        return await _db.ProviderSettings
            .FirstOrDefaultAsync(s => s.Provider == p && s.Key == k, ct);
    }

    public async Task<IReadOnlyList<ProviderSetting>> GetAllForProviderAsync(
        string provider, CancellationToken ct = default)
    {
        var p = provider.ToLowerInvariant();
        return await _db.ProviderSettings
            .Where(s => s.Provider == p)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(ProviderSetting setting, CancellationToken ct = default)
    {
        var existing = await GetAsync(setting.Provider, setting.Key, ct);

        if (existing is null)
            await _db.ProviderSettings.AddAsync(setting, ct);
        else
            existing.UpdateValue(setting.EncryptedValue);
    }
}
