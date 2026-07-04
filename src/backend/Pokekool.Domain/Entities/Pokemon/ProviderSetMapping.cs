using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

/// <summary>
/// Mappe un set Pokémon vers l'identifiant externe d'un provider.
/// Remplace les colonnes pricecharting_slug, cm_expansion_id, tcgplayer_group_id
/// sur pokemon_sets pour permettre l'ajout de nouveaux providers sans migration.
/// </summary>
public sealed class ProviderSetMapping : Entity
{
    public Guid   SetId    { get; private set; }
    public string Provider { get; private set; } = default!;   // ex: "pricecharting"
    public string Key      { get; private set; } = default!;   // ex: "slug"
    public string Value    { get; private set; } = default!;

    // Navigation (optionnelle — chargée si besoin)
    public PokemonSet? Set { get; private set; }

    private ProviderSetMapping() { }

    public static ProviderSetMapping Create(Guid setId, string provider, string key, string value)
    {
        if (setId == Guid.Empty)            throw new ArgumentException("SetId is required", nameof(setId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required", nameof(provider));
        if (string.IsNullOrWhiteSpace(key))      throw new ArgumentException("Key is required", nameof(key));

        return new ProviderSetMapping
        {
            Id        = Guid.NewGuid(),
            SetId     = setId,
            Provider  = provider.ToLowerInvariant().Trim(),
            Key       = key.ToLowerInvariant().Trim(),
            Value     = value.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateValue(string value)
    {
        Value     = value.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
