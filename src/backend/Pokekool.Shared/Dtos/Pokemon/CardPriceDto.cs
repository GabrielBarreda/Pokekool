namespace Pokekool.Shared.Dtos.Pokemon;

public sealed record CardPriceDto(
    string Source,
    string? Variant,
    decimal? MarketPrice,
    decimal? LowPrice,
    decimal? HighPrice,
    string Currency,
    DateTime FetchedAt);
