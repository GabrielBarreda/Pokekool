using System.Text.Json.Serialization;

namespace Pokekool.Infrastructure.DataProviders.TcgPlayer;

// ── Enveloppe générique TCGPlayer ─────────────────────────────────────────────

internal sealed record TcgPlayerResponse<T>(
    [property: JsonPropertyName("results")] List<T> Results);

// ── Token OAuth 2.0 ──────────────────────────────────────────────────────────

internal sealed record TcgPlayerTokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("expires_in")]    int    ExpiresIn,
    [property: JsonPropertyName("token_type")]    string TokenType);

// ── Sets (Groups) ─────────────────────────────────────────────────────────────

internal sealed record TcgPlayerSet(
    [property: JsonPropertyName("groupId")]      int     GroupId,
    [property: JsonPropertyName("name")]         string  Name,
    [property: JsonPropertyName("abbreviation")] string? Abbreviation,
    [property: JsonPropertyName("publishedOn")]  string? PublishedOn,
    [property: JsonPropertyName("categoryId")]   int     CategoryId);

// ── Produits (cartes) ─────────────────────────────────────────────────────────

internal sealed record TcgPlayerProduct(
    [property: JsonPropertyName("productId")]   int     ProductId,
    [property: JsonPropertyName("name")]        string  Name,
    [property: JsonPropertyName("cleanName")]   string? CleanName,
    [property: JsonPropertyName("groupId")]     int     GroupId,
    [property: JsonPropertyName("categoryId")]  int     CategoryId,
    [property: JsonPropertyName("url")]         string? Url);

// ── Prix ──────────────────────────────────────────────────────────────────────

internal sealed record TcgPlayerPrice(
    [property: JsonPropertyName("productId")]       int      ProductId,
    [property: JsonPropertyName("lowPrice")]        decimal? LowPrice,
    [property: JsonPropertyName("midPrice")]        decimal? MidPrice,
    [property: JsonPropertyName("highPrice")]       decimal? HighPrice,
    [property: JsonPropertyName("marketPrice")]     decimal? MarketPrice,
    [property: JsonPropertyName("directLowPrice")]  decimal? DirectLowPrice,
    [property: JsonPropertyName("subTypeName")]     string?  SubTypeName);
