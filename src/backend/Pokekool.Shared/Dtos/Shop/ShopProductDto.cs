namespace Pokekool.Shared.Dtos.Shop;

public sealed record ShopProductDto(
    Guid Id,
    Guid ShopId,
    string Type,
    Guid? CardId,
    string? CardName,
    string? CardImage,
    string? ProductName,
    string? Condition,
    string? GradingCompany,
    decimal? GradeValue,
    decimal Price,
    string Currency,
    int Quantity,
    bool IsAvailable,
    string? Description,
    string? PhotoUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
