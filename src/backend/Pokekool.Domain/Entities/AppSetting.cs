namespace Pokekool.Domain.Entities;

/// <summary>
/// Paramètre applicatif générique (clé/valeur, non chiffré) — réservé aux réglages
/// non sensibles lus par l'UI (à l'inverse de <see cref="ProviderSetting"/>, chiffré et
/// propre aux providers externes). Clé = identifiant stable (ex. "storage.cascade_delete").
/// </summary>
public sealed class AppSetting
{
    /// <summary>Clé stable du paramètre (ex. "storage.cascade_delete").</summary>
    public string Key { get; private set; } = default!;

    /// <summary>Valeur brute (ex. "true" / "false", un nombre, un JSON court…).</summary>
    public string Value { get; private set; } = default!;

    public DateTimeOffset UpdatedAt { get; private set; }

    private AppSetting() { }

    public static AppSetting Create(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key requis", nameof(key));
        return new AppSetting
        {
            Key = key.Trim(),
            Value = value ?? string.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetValue(string value)
    {
        Value = value ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
