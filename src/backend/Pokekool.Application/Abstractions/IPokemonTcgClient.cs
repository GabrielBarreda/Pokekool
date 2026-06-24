namespace Pokekool.Application.Abstractions;

public interface IPokemonTcgClient
{
    Task<IReadOnlyList<TcgSetDto>> GetAllSetsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TcgCardDto>> GetCardsBySetAsync(string setId, CancellationToken ct = default);
}

public sealed record TcgSetDto(
    string Id,
    string Name,
    string? Series,
    string? ReleaseDate,
    int? Total,
    int? PrintedTotal,
    string? SymbolUrl,
    string? LogoUrl);

public sealed record TcgCardDto(
    string Id,
    string Name,
    string Number,
    string? Supertype,
    string[]? Subtypes,
    string? Rarity,
    string[]? Types,
    int? Hp,
    string? ImageSmall,
    string? ImageLarge,
    TcgCardPricesDto? Prices = null);

// ---- Prix ----

public sealed record TcgCardPricesDto(
    TcgPlayerPricesDto? TcgPlayer,
    CardMarketPricesDto? CardMarket);

public sealed record TcgPlayerPricesDto(
    TcgPriceVariantDto? Normal,
    TcgPriceVariantDto? Holofoil,
    TcgPriceVariantDto? ReverseHolofoil);

public sealed record TcgPriceVariantDto(decimal? Low, decimal? Mid, decimal? High, decimal? Market);

public sealed record CardMarketPricesDto(decimal? AverageSellPrice, decimal? LowPrice);
