namespace Pokekool.Infrastructure.DataProviders.PriceCharting;

/// <summary>
/// Construit les URLs PriceCharting depuis un TcgId ("base1-4").
/// Table de mapping hardcodée pour les sets principaux.
/// À terme, la valeur viendra de PokemonSet.PriceChartingSlug.
/// </summary>
public static class PriceChartingUrlBuilder
{
    private const string BaseUrl = "https://www.pricecharting.com/game";

    // TcgId set prefix → PriceCharting slug
    private static readonly Dictionary<string, string> SetSlugMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Base
            { "base1",    "pokemon-base-set" },
            { "base2",    "pokemon-jungle" },
            { "base3",    "pokemon-fossil" },
            { "base4",    "pokemon-base-set-2" },
            { "base5",    "pokemon-team-rocket" },
            { "base6",    "pokemon-legendary-collection" },

            // Gym
            { "gym1",     "pokemon-gym-heroes" },
            { "gym2",     "pokemon-gym-challenge" },

            // Neo
            { "neo1",     "pokemon-neo-genesis" },
            { "neo2",     "pokemon-neo-discovery" },
            { "neo3",     "pokemon-neo-revelation" },
            { "neo4",     "pokemon-neo-destiny" },

            // e-Card
            { "ecard1",   "pokemon-expedition-base-set" },
            { "ecard2",   "pokemon-aquapolis" },
            { "ecard3",   "pokemon-skyridge" },

            // EX
            { "ex1",      "pokemon-ruby-and-sapphire" },
            { "ex2",      "pokemon-sandstorm" },
            { "ex3",      "pokemon-dragon" },
            { "ex4",      "pokemon-team-magma-vs-team-aqua" },
            { "ex5",      "pokemon-hidden-legends" },
            { "ex6",      "pokemon-firered-and-leafgreen" },
            { "ex7",      "pokemon-team-rocket-returns" },
            { "ex8",      "pokemon-deoxys" },
            { "ex9",      "pokemon-emerald" },
            { "ex10",     "pokemon-unseen-forces" },
            { "ex11",     "pokemon-delta-species" },
            { "ex12",     "pokemon-legend-maker" },
            { "ex13",     "pokemon-holon-phantoms" },
            { "ex14",     "pokemon-crystal-guardians" },
            { "ex15",     "pokemon-dragon-frontiers" },
            { "ex16",     "pokemon-power-keepers" },

            // Diamond & Pearl
            { "dp1",      "pokemon-diamond-and-pearl" },
            { "dp2",      "pokemon-mysterious-treasures" },
            { "dp3",      "pokemon-secret-wonders" },
            { "dp4",      "pokemon-great-encounters" },
            { "dp5",      "pokemon-majestic-dawn" },
            { "dp6",      "pokemon-legends-awakened" },
            { "dp7",      "pokemon-stormfront" },

            // Platinum
            { "pl1",      "pokemon-platinum" },
            { "pl2",      "pokemon-rising-rivals" },
            { "pl3",      "pokemon-supreme-victors" },
            { "pl4",      "pokemon-arceus" },

            // HeartGold SoulSilver
            { "hgss1",    "pokemon-heartgold-soulsilver" },
            { "hgss2",    "pokemon-unleashed" },
            { "hgss3",    "pokemon-undaunted" },
            { "hgss4",    "pokemon-triumphant" },

            // Black & White
            { "bw1",      "pokemon-black-and-white" },
            { "bw2",      "pokemon-emerging-powers" },
            { "bw3",      "pokemon-noble-victories" },
            { "bw4",      "pokemon-next-destinies" },
            { "bw5",      "pokemon-dark-explorers" },
            { "bw6",      "pokemon-dragons-exalted" },
            { "bw7",      "pokemon-boundaries-crossed" },
            { "bw8",      "pokemon-plasma-storm" },
            { "bw9",      "pokemon-plasma-freeze" },
            { "bw10",     "pokemon-plasma-blast" },
            { "bw11",     "pokemon-legendary-treasures" },

            // XY
            { "xy1",      "pokemon-xy" },
            { "xy2",      "pokemon-flashfire" },
            { "xy3",      "pokemon-furious-fists" },
            { "xy4",      "pokemon-phantom-forces" },
            { "xy5",      "pokemon-primal-clash" },
            { "xy6",      "pokemon-roaring-skies" },
            { "xy7",      "pokemon-ancient-origins" },
            { "xy8",      "pokemon-breakthrough" },
            { "xy9",      "pokemon-breakpoint" },
            { "xy10",     "pokemon-fates-collide" },
            { "xy11",     "pokemon-steam-siege" },
            { "xy12",     "pokemon-evolutions" },

            // Sun & Moon
            { "sm1",      "pokemon-sun-and-moon" },
            { "sm2",      "pokemon-guardians-rising" },
            { "sm3",      "pokemon-burning-shadows" },
            { "sm35",     "pokemon-shining-legends" },
            { "sm4",      "pokemon-crimson-invasion" },
            { "sm5",      "pokemon-ultra-prism" },
            { "sm6",      "pokemon-forbidden-light" },
            { "sm7",      "pokemon-celestial-storm" },
            { "sm75",     "pokemon-dragon-majesty" },
            { "sm8",      "pokemon-lost-thunder" },
            { "sm9",      "pokemon-team-up" },
            { "sm10",     "pokemon-unbroken-bonds" },
            { "sm11",     "pokemon-unified-minds" },
            { "sm115",    "pokemon-hidden-fates" },
            { "sm12",     "pokemon-cosmic-eclipse" },

            // Sword & Shield
            { "swsh1",    "pokemon-sword-and-shield" },
            { "swsh2",    "pokemon-rebel-clash" },
            { "swsh3",    "pokemon-darkness-ablaze" },
            { "swsh35",   "pokemon-champions-path" },
            { "swsh4",    "pokemon-vivid-voltage" },
            { "swsh45",   "pokemon-shining-fates" },
            { "swsh5",    "pokemon-battle-styles" },
            { "swsh6",    "pokemon-chilling-reign" },
            { "swsh7",    "pokemon-evolving-skies" },
            { "swsh8",    "pokemon-fusion-strike" },
            { "swsh9",    "pokemon-brilliant-stars" },
            { "swsh10",   "pokemon-astral-radiance" },
            { "swsh11",   "pokemon-lost-origin" },
            { "swsh12",   "pokemon-silver-tempest" },
            { "swsh12pt5","pokemon-crown-zenith" },

            // Scarlet & Violet
            { "sv1",      "pokemon-scarlet-and-violet" },
            { "sv2",      "pokemon-paldea-evolved" },
            { "sv3",      "pokemon-obsidian-flames" },
            { "sv3pt5",   "pokemon-151" },
            { "sv4",      "pokemon-paradox-rift" },
            { "sv4pt5",   "pokemon-paldean-fates" },
            { "sv5",      "pokemon-temporal-forces" },
            { "sv6",      "pokemon-twilight-masquerade" },
            { "sv6pt5",   "pokemon-shrouded-fable" },
            { "sv7",      "pokemon-stellar-crown" },
            { "sv8",      "pokemon-surging-sparks" },
            { "sv8pt5",   "pokemon-prismatic-evolutions" },
        };

    /// <summary>
    /// Construit l'URL PriceCharting depuis un TcgId ("base1-4").
    ///
    /// Priorité du slug set :
    ///   1. <paramref name="priceChartingSlugOverride"/> (colonne DB pricecharting_slug)
    ///   2. Table hardcodée <see cref="SetSlugMap"/> (sets connus)
    ///   3. Génération automatique depuis <paramref name="setName"/> → "pokemon-{slug}"
    ///      (fallback pour les sets récents absents de la map)
    /// </summary>
    /// <summary>
    /// Construit l'URL PriceCharting depuis un TcgId.
    ///
    /// Format : /game/{setSlug}/{cardNameSlug}-{number}
    ///   Ex : /game/pokemon-base-set/charizard-4
    ///
    /// Priorité du slug set :
    ///   1. <paramref name="priceChartingSlugOverride"/> (colonne DB)
    ///   2. Table hardcodée <see cref="SetSlugMap"/>
    ///   3. Génération depuis <paramref name="setName"/> → "pokemon-{slug}"
    /// </summary>
    public static string? BuildUrl(
        string  tcgId,
        string? cardName                  = null,
        string? priceChartingSlugOverride = null,
        string? setName                   = null)
    {
        if (string.IsNullOrWhiteSpace(tcgId)) return null;

        var lastDash = tcgId.LastIndexOf('-');
        if (lastDash <= 0) return null;

        var setPrefix  = tcgId[..lastDash];
        var cardNumber = tcgId[(lastDash + 1)..];

        // ── Slug du set ──────────────────────────────────────────────────────
        var setSlug = priceChartingSlugOverride;

        if (string.IsNullOrWhiteSpace(setSlug))
            SetSlugMap.TryGetValue(setPrefix, out setSlug);

        if (string.IsNullOrWhiteSpace(setSlug) && !string.IsNullOrWhiteSpace(setName))
            setSlug = "pokemon-" + ToSlug(setName);

        if (string.IsNullOrWhiteSpace(setSlug)) return null;

        // ── Slug de la carte : "{name}-{number}"  (ex: charizard-4) ─────────
        var cardSlug = !string.IsNullOrWhiteSpace(cardName)
            ? $"{ToSlug(cardName)}-{cardNumber}"
            : cardNumber;

        return $"{BaseUrl}/{setSlug}/{cardSlug}";
    }

    /// <summary>
    /// Retourne le slug hardcodé pour un prefix de set, ou null si inconnu.
    /// </summary>
    public static string? GetSetSlug(string setTcgIdPrefix)
        => SetSlugMap.TryGetValue(setTcgIdPrefix, out var slug) ? slug : null;

    // ── Slug generation ───────────────────────────────────────────────────────

    private static string ToSlug(string input)
    {
        // Supprimer accents
        var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var s = sb.ToString().Normalize(System.Text.NormalizationForm.FormC)
            .ToLowerInvariant()
            .Replace("—", "-").Replace("–", "-").Replace("&", "and").Replace("'", "");

        // Espaces → tirets, supprimer non-alphanum sauf tirets
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[\s_]+", "-");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-]", "");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"-{2,}", "-").Trim('-');

        return s;
    }
}
