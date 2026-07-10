namespace Pokekool.Shared.Requests.Shop;

/// <summary>Corps de création / modification d'une entrée de stock.</summary>
public sealed record ShopStockRequest(
    Guid? CardId,
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
    string? Notes);
