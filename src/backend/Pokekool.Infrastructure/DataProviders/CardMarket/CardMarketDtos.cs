using System.Text.Json.Serialization;

namespace Pokekool.Infrastructure.DataProviders.CardMarket;

// ── Expansions ─────────────────────────────────────────────────────────────

public sealed record CmExpansionsResponse(
    [property: JsonPropertyName("expansion")] List<CmExpansion> Expansions);

public sealed record CmExpansion(
    [property: JsonPropertyName("idExpansion")] int IdExpansion,
    [property: JsonPropertyName("enName")]      string EnName,
    [property: JsonPropertyName("abbreviation")]string Abbreviation);

// ── Singles (cartes d'une expansion) ──────────────────────────────────────

public sealed record CmSinglesResponse(
    [property: JsonPropertyName("single")] List<CmSingle> Singles);

public sealed record CmSingle(
    [property: JsonPropertyName("idProduct")]  int    IdProduct,
    [property: JsonPropertyName("enName")]     string EnName,
    [property: JsonPropertyName("number")]     string Number,
    [property: JsonPropertyName("priceGuide")] CmPriceGuide? PriceGuide);

// ── Price guide (champs retournés sur /singles et /priceguide) ─────────────

public sealed record CmPriceGuide(
    [property: JsonPropertyName("SELL")]    decimal? Sell,
    [property: JsonPropertyName("LOW")]     decimal? Low,
    [property: JsonPropertyName("LOWEX")]   decimal? LowEx,
    [property: JsonPropertyName("LOWFOIL")] decimal? LowFoil,
    [property: JsonPropertyName("AVG")]     decimal? Avg,
    [property: JsonPropertyName("TREND")]   decimal? Trend,
    [property: JsonPropertyName("AVG1")]    decimal? Avg1,
    [property: JsonPropertyName("AVG7")]    decimal? Avg7,
    [property: JsonPropertyName("AVG30")]   decimal? Avg30);
