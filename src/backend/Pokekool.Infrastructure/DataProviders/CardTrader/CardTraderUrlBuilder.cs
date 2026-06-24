using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pokekool.Infrastructure.DataProviders.CardTrader;

/// <summary>
/// Construit les URLs de recherche CardTrader depuis les métadonnées d'une carte.
///
/// Pattern : https://www.cardtrader.com/cards?q={card}+{set}&game_id=5
///   game_id=5 → filtre Pokémon TCG
///
/// Exemple : Charizard, Base Set
///   → https://www.cardtrader.com/cards?q=Charizard+Base+Set&game_id=5
/// </summary>
public static class CardTraderUrlBuilder
{
    private const string BaseUrl = "https://www.cardtrader.com/cards";
    private const string GameId  = "5"; // Pokémon TCG

    public static string? BuildUrl(string? cardName, string? setName)
    {
        if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(setName))
            return null;

        var query = Uri.EscapeDataString($"{Normalize(cardName)} {Normalize(setName)}");
        return $"{BaseUrl}?q={query}&game_id={GameId}";
    }

    private static string Normalize(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
