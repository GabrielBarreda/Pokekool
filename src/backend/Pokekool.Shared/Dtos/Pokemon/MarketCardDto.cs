namespace Pokekool.Shared.Dtos.Pokemon;

public sealed record MarketCardDto(
    Guid Id,
    string TcgId,
    string Number,
    string Name,
    string DisplayName,
    string? Supertype,
    string? Rarity,
    string[]? Types,
    string? ImageSmall,
    string? ImageLarge,
    decimal? PriceMarket,       // meilleur prix marché toutes sources
    decimal? PriceLow,
    string? PriceCurrency,
    string? PriceSource,
    int AlertCount);             // nb d'alertes actives (0 pour l'instant)
