namespace Pokekool.Domain.Entities.Pokemon;

/// <summary>
/// Constantes pour les noms de providers et leurs clés de mapping.
/// Évite les magic strings dans les providers et repositories.
/// </summary>
public static class ProviderKeys
{
    // ── Noms de providers ────────────────────────────────────────────────────
    public const string PriceCharting    = "pricecharting";
    public const string CardMarket       = "cardmarket";
    public const string TcgPlayer        = "tcgplayer";
    public const string GemTracker       = "gemtracker";

    // ── Clés set-level ───────────────────────────────────────────────────────
    /// <summary>Slug PriceCharting du set — ex: "pokemon-base-set"</summary>
    public const string Slug        = "slug";

    /// <summary>ID expansion CardMarket — ex: "1234"</summary>
    public const string ExpansionId = "expansion_id";

    /// <summary>Group ID TCGPlayer — ex: "1"</summary>
    public const string GroupId     = "group_id";

    /// <summary>Ère GemTracker — ex: "xy", "sun-and-moon"</summary>
    public const string Era         = "era";

    /// <summary>ID numérique GemTracker du set — ex: "20071"</summary>
    public const string GemSetId    = "set_id";

    /// <summary>Slug GemTracker du set — ex: "generations"</summary>
    public const string GemSetSlug  = "set_slug";

    // ── Clés card-level ──────────────────────────────────────────────────────
    /// <summary>ID produit (CardMarket ou TCGPlayer)</summary>
    public const string ProductId    = "product_id";

    /// <summary>Slug de scraping CardMarket (override manuel)</summary>
    public const string ScrapingSlug = "scraping_slug";
}
