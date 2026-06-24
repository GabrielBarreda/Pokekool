using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pokekool.Infrastructure.DataProviders.CardMarketScraping;

/// <summary>
/// Construit les URLs de produit CardMarket depuis les métadonnées d'une carte TCG.
///
/// Pattern : https://www.cardmarket.com/fr/Pokemon/Products/Singles/{set-slug}/{card-slug}
///
/// Exemple : Charizard, Base Set
///   → https://www.cardmarket.com/fr/Pokemon/Products/Singles/Base-Set/Charizard
///
/// Règles de slug :
///   - accents supprimés (NFD + strip non-ASCII)
///   - espaces et underscores → tirets
///   - caractères non alphanumériques (hors tiret) supprimés
///   - tirets multiples fusionnés, tirets en début/fin supprimés
/// </summary>
public static class CardMarketScrapingUrlBuilder
{
    private const string BaseUrl = "https://www.cardmarket.com/fr/Pokemon/Products/Singles";

    /// <summary>
    /// Construit l'URL du produit CardMarket.
    /// </summary>
    /// <param name="setName">Nom du set TCG (ex: "Base Set", "Scarlet &amp; Violet—151").</param>
    /// <param name="cardName">Nom de la carte (ex: "Charizard", "Blastoise ex").</param>
    /// <param name="cardSlugOverride">Slug carte stocké en BDD (cm_scraping_slug), prioritaire.</param>
    /// <returns>URL complète, ou null si les entrées sont invalides.</returns>
    public static string? BuildUrl(string? setName, string? cardName, string? cardSlugOverride = null)
    {
        if (string.IsNullOrWhiteSpace(setName) || string.IsNullOrWhiteSpace(cardName))
            return null;

        var setSlug  = ToSlug(setName);
        var cardSlug = string.IsNullOrWhiteSpace(cardSlugOverride)
            ? ToSlug(cardName)
            : cardSlugOverride.Trim();

        if (string.IsNullOrWhiteSpace(setSlug) || string.IsNullOrWhiteSpace(cardSlug))
            return null;

        return $"{BaseUrl}/{setSlug}/{cardSlug}";
    }

    /// <summary>
    /// Transforme un nom libre en slug compatible CardMarket :
    ///   "Base Set" → "Base-Set"   |   "Scarlet &amp; Violet" → "Scarlet-Violet"
    ///   "Pikachu V" → "Pikachu-V" |   "Gardevoir ex" → "Gardevoir-ex"
    /// </summary>
    public static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // 1. Normaliser en NFD pour séparer les caractères de base de leurs diacritiques
        var normalized = input.Normalize(NormalizationForm.FormD);

        // 2. Supprimer les caractères de catégorie "Non-Spacing Mark" (accents, etc.)
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var withoutAccents = sb.ToString().Normalize(NormalizationForm.FormC);

        // 3. Remplacer & et symboles similaires par rien
        withoutAccents = withoutAccents
            .Replace("&", " ")
            .Replace("'", "")
            .Replace("'", "")
            .Replace("—", "-")
            .Replace("–", "-");

        // 4. Remplacer espaces et underscores par tirets
        var dashed = Regex.Replace(withoutAccents, @"[\s_]+", "-");

        // 5. Supprimer tout ce qui n'est pas alphanumérique ou tiret
        var cleaned = Regex.Replace(dashed, @"[^a-zA-Z0-9\-]", "");

        // 6. Fusionner les tirets multiples et supprimer les tirets de bord
        var slug = Regex.Replace(cleaned, @"-{2,}", "-").Trim('-');

        return slug;
    }
}
