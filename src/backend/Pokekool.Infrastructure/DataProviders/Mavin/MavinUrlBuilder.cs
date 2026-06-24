namespace Pokekool.Infrastructure.DataProviders.Mavin;

/// <summary>
/// Construit les URLs de recherche Mavin.io depuis les métadonnées d'une carte.
/// Mavin agrège les ventes eBay complétées sans nécessiter d'accès direct à l'API eBay.
///
/// Pattern : https://mavin.io/search?q={card}+{set}+pokemon+card&cat=CCG+Individual+Cards
///
/// Exemple : Charizard, Base Set
///   → https://mavin.io/search?q=Charizard+Base+Set+pokemon+card&cat=CCG+Individual+Cards
/// </summary>
public static class MavinUrlBuilder
{
    private const string BaseUrl  = "https://mavin.io/search";
    private const string Category = "CCG Individual Cards";

    public static string? BuildUrl(string? cardName, string? setName)
    {
        if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(setName))
            return null;

        var q   = Uri.EscapeDataString($"{cardName} {setName} pokemon card");
        var cat = Uri.EscapeDataString(Category);
        return $"{BaseUrl}?q={q}&cat={cat}";
    }
}
