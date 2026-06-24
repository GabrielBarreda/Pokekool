namespace Pokekool.Domain.Entities;

/// <summary>
/// Paramètre de configuration d'un provider externe (CardMarket, PriceCharting...).
/// La valeur est stockée chiffrée en base (AES via ASP.NET Data Protection).
/// Clé composite : (Provider, Key).
/// </summary>
public sealed class ProviderSetting
{
    /// <summary>Identifiant du provider. Ex: "cardmarket", "pricecharting".</summary>
    public string Provider { get; private set; } = default!;

    /// <summary>Nom de la clé de configuration. Ex: "app_token", "app_secret".</summary>
    public string Key { get; private set; } = default!;

    /// <summary>Valeur chiffrée (ne jamais exposer directement via l'API).</summary>
    public string EncryptedValue { get; private set; } = default!;

    public DateTimeOffset UpdatedAt { get; private set; }

    private ProviderSetting() { }

    public static ProviderSetting Create(string provider, string key, string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider requis", nameof(provider));
        if (string.IsNullOrWhiteSpace(key))      throw new ArgumentException("Key requis",      nameof(key));
        if (string.IsNullOrWhiteSpace(encryptedValue)) throw new ArgumentException("Value requis", nameof(encryptedValue));

        return new ProviderSetting
        {
            Provider       = provider.ToLowerInvariant().Trim(),
            Key            = key.ToLowerInvariant().Trim(),
            EncryptedValue = encryptedValue,
            UpdatedAt      = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateValue(string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue)) throw new ArgumentException("Value requis", nameof(encryptedValue));
        EncryptedValue = encryptedValue;
        UpdatedAt      = DateTimeOffset.UtcNow;
    }
}
