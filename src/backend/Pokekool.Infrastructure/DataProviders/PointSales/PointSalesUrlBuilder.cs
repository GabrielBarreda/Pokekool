namespace Pokekool.Infrastructure.DataProviders.PointSales;

/// <summary>
/// Construit les URLs de recherche 130point.com depuis les métadonnées d'une carte.
/// 130point agrège les ventes eBay complétées (données publiques).
///
/// Pattern : https://130point.com/sales/?s={name}+{number}+{set}+pokemon+card
///
/// Le numéro de carte est inclus pour réduire le bruit (éviter Charizard SP, ex, etc.)
/// Exemple : Charizard, 4, Base Set
///   → https://130point.com/sales/?s=Charizard+%234+Base+Set+pokemon+card
/// </summary>
public static class PointSalesUrlBuilder
{
    private const string BaseUrl = "https://130point.com/sales/";

    public static string? BuildUrl(string? cardName, string? setName, string? cardNumber = null)
    {
        if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(setName))
            return null;

        // Inclure le numéro (#4) pour cibler la carte précise plutôt que toutes les variantes
        var query = string.IsNullOrWhiteSpace(cardNumber)
            ? $"{cardName} {setName} pokemon card"
            : $"{cardName} #{cardNumber} {setName} pokemon card";

        var q = Uri.EscapeDataString(query);
        return $"{BaseUrl}?s={q}";
    }
}
