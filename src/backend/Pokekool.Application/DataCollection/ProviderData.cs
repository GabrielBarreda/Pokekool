namespace Pokekool.Application.DataCollection;

public sealed record CardSetData(
    string ProviderSetId,
    string Name,
    string? Series,
    DateOnly? ReleaseDate,
    int? TotalCards,
    string? SymbolUrl,
    string? LogoUrl);

public sealed record CardData(
    string ProviderCardId,
    string SetId,
    string Number,
    string Name,
    string? Supertype,
    string[]? Subtypes,
    string? Rarity,
    string[]? Types,
    int? Hp,
    string? ImageSmall,
    string? ImageLarge,
    CardPriceData? Prices);

public sealed record CardPriceData(
    string Source,
    decimal? RawMarketPrice,
    decimal? Psa7,
    decimal? Psa8,
    decimal? Psa9,
    decimal? Psa10,
    string Currency,
    DateTime FetchedAt);

public sealed record CardPopulationData(
    string Source,
    int? Total,
    int? Pop10,
    int? Pop9,
    int? Pop8,
    int? Pop7,
    double? GemRate10,   // Pop10 / Total * 100
    DateTime FetchedAt);
