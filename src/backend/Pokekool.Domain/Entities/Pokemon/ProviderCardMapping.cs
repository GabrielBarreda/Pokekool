using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

/// <summary>
/// Mappe une carte Pokémon vers l'identifiant externe d'un provider.
/// Remplace les colonnes cm_product_id, cm_scraping_slug, tcgplayer_product_id
/// sur pokemon_cards pour permettre l'ajout de nouveaux providers sans migration.
/// </summary>
public sealed class ProviderCardMapping : Entity
{
    public Guid   CardId   { get; private set; }
    public string Provider { get; private set; } = default!;   // ex: "tcgplayer"
    public string Key      { get; private set; } = default!;   // ex: "product_id"
    public string Value    { get; private set; } = default!;

    // Navigation (optionnelle)
    public PokemonCard? Card { get; private set; }

    private ProviderCardMapping() { }

    public static ProviderCardMapping Create(Guid cardId, string provider, string key, string value)
    {
        if (cardId == Guid.Empty)            throw new ArgumentException("CardId is required", nameof(cardId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required", nameof(provider));
        if (string.IsNullOrWhiteSpace(key))      throw new ArgumentException("Key is required", nameof(key));

        return new ProviderCardMapping
        {
            Id        = Guid.NewGuid(),
            CardId    = cardId,
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
