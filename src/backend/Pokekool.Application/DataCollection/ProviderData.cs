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

/// <summary>
/// Prix d'une carte retourné par un provider.
/// Les prix gradés sont dans <see cref="GradedPrices"/> — une entrée par (société, note).
/// </summary>
public sealed record CardPriceData(
    string Source,
    decimal? RawMarketPrice,
    IReadOnlyList<GradedPriceData>? GradedPrices = null,
    string   Currency  = "USD",
    DateTime FetchedAt = default);

/// <summary>
/// Prix pour un grade précis d'une société de notation.
/// </summary>
/// <param name="GradingCompany">Ex : "PSA", "BGS", "CGC", "SGC"</param>
/// <param name="GradeValue">Ex : 10m, 9.5m, 9m, 1m …</param>
/// <param name="Price">Prix en devise <see cref="CardPriceData.Currency"/></param>
public sealed record GradedPriceData(
    string  GradingCompany,
    decimal GradeValue,
    decimal Price);

public sealed record CardPopulationData(
    string Source,
    int? Total,
    int? Pop10,
    int? Pop9,
    int? Pop8,
    int? Pop7,
    double? GemRate10,   // Pop10 / Total * 100
    DateTime FetchedAt);

/// <summary>
/// Contexte passé à chaque appel <see cref="IPopulationProvider.GetPopulationAsync"/>.
/// Contient toutes les infos dont un provider peut avoir besoin (identifiants, noms, numéro de carte).
/// </summary>
public sealed record CardPopulationRequest(
    Guid   CardId,
    string CardName,
    string CardNumber,
    Guid   SetId,
    string SetName);
