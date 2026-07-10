namespace Pokekool.Shared.Dtos.Shop;

public sealed record ShopStockDto(
    Guid Id,
    Guid ShopId,
    Guid? CardId,
    string? CardName,
    string? CardImage,
    string? ProductName,
    string Type,
    string? Condition,
    string? GradingCompany,
    decimal? GradeValue,
    int QuantityTotal,
    int QuantityAvailable,
    int QuantityReserved,
    decimal? PurchasePrice,
    string? PurchaseCurrency,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
