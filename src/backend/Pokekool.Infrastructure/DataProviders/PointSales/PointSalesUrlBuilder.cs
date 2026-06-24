namespace Pokekool.Infrastructure.DataProviders.PointSales;

/// <summary>
/// Construit les URLs de recherche 130point.com depuis les métadonnées d'une carte.
/// 130point agrège les ventes eBay complétées (données publiques).
///
/// Pattern : https://130point.com/sales/?s={card}+{set}+pokemon
///
/// Exemple : Charizard, Base Set
///   → https://130point.com/sales/?s=Charizard+Base+Set+pokemon
/// </summary>
public static class PointSalesUrlBuilder
{
    private const string BaseUrl = "https://130point.com/sales/";

    public static string? BuildUrl(string? cardName, string? setName)
    {
        if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(setName))
            return null;

        var q = Uri.EscapeDataString($"{cardName} {setName} pokemon");
        return $"{BaseUrl}?s={q}";
    }
}
