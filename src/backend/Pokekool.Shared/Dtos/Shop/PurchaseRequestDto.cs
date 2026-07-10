namespace Pokekool.Shared.Dtos.Shop;

public sealed record PurchaseRequestDto(
    Guid Id,
    Guid ShopProductId,
    string? ProductName,
    decimal ProductPrice,
    string ProductCurrency,
    Guid UserId,
    string ClientName,
    string ClientEmail,
    string? Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
