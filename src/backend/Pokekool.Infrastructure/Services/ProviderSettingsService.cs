using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Services;

/// <summary>
/// Chiffrement via ASP.NET Data Protection (AES-256-CBC + HMACSHA256).
/// Cache mémoire 5 min par provider pour éviter les aller-retours DB sur chaque requête API.
/// </summary>
internal sealed class ProviderSettingsService : IProviderSettingsService
{
    private const string Purpose = "pokekool.provider_settings.v1";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IProviderSettingRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IDataProtector _protector;
    private readonly IMemoryCache _cache;

    public ProviderSettingsService(
        IProviderSettingRepository repo,
        IUnitOfWork uow,
        IDataProtectionProvider dataProtection,
        IMemoryCache cache)
    {
        _repo      = repo;
        _uow       = uow;
        _protector = dataProtection.CreateProtector(Purpose);
        _cache     = cache;
    }

    public async Task<string?> GetAsync(string provider, string key, CancellationToken ct = default)
    {
        var cacheKey = CacheKey(provider, key);

        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var setting = await _repo.GetAsync(provider, key, ct);
        if (setting is null) return null;

        var plainValue = _protector.Unprotect(setting.EncryptedValue);
        _cache.Set(cacheKey, plainValue, CacheTtl);
        return plainValue;
    }

    public async Task SetAsync(string provider, string key, string plainValue, CancellationToken ct = default)
    {
        var encrypted = _protector.Protect(plainValue);
        var setting   = ProviderSetting.Create(provider, key, encrypted);

        await _repo.UpsertAsync(setting, ct);
        await _uow.SaveChangesAsync(ct);

        // Invalide le cache pour que le prochain GetAsync lise la nouvelle valeur
        InvalidateCache(provider, key);
    }

    public async Task<IReadOnlyList<string>> GetConfiguredKeysAsync(
        string provider, CancellationToken ct = default)
    {
        var settings = await _repo.GetAllForProviderAsync(provider, ct);
        return settings.Select(s => s.Key).ToList();
    }

    public void InvalidateCache(string provider)
    {
        // On ne connaît pas les clés individuelles — le provider sera rechargé progressivement
        // À améliorer si besoin avec un CancellationTokenSource par provider
    }

    private static void InvalidateCache(IMemoryCache cache, string provider, string key)
        => cache.Remove(CacheKey(provider, key));

    private void InvalidateCache(string provider, string key)
        => _cache.Remove(CacheKey(provider, key));

    private static string CacheKey(string provider, string key)
        => $"provider_settings:{provider.ToLowerInvariant()}:{key.ToLowerInvariant()}";
}
