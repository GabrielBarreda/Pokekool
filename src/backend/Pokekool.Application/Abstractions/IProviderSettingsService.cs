namespace Pokekool.Application.Abstractions;

/// <summary>
/// Service d'accès aux paramètres chiffrés des providers externes.
/// Gère le chiffrement/déchiffrement et le cache mémoire.
/// </summary>
public interface IProviderSettingsService
{
    /// <summary>Retourne la valeur déchiffrée, ou null si non configuré.</summary>
    Task<string?> GetAsync(string provider, string key, CancellationToken ct = default);

    /// <summary>Chiffre et persiste la valeur. Invalide le cache du provider.</summary>
    Task SetAsync(string provider, string key, string plainValue, CancellationToken ct = default);

    /// <summary>
    /// Retourne toutes les clés configurées pour ce provider.
    /// Les valeurs sont masquées (null) — on expose seulement si la clé est configurée.
    /// </summary>
    Task<IReadOnlyList<string>> GetConfiguredKeysAsync(string provider, CancellationToken ct = default);

    /// <summary>Vide le cache pour un provider (appelé après UpsertProviderSettingCommand).</summary>
    void InvalidateCache(string provider);
}
