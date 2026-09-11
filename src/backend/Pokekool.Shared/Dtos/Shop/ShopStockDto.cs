namespace Pokekool.Shared.Dtos.Shop;

public sealed record ShopStockDto(
    Guid Id,
    Guid ShopId,
    Guid? CardId,
    string? CardTcgId,
    string? CardName,
    string? DisplayName,
    string? CardImage,
    string? ProductName,
    string Type,
    string Language,
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
