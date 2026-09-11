namespace Pokekool.Shared.Dtos.Shop;

public sealed record ShopProductDto(
    Guid Id,
    Guid ShopId,
    string Type,
    Guid? CardId,
    string? CardTcgId,
    string? CardName,
    string? DisplayName,
    string? CardImage,
    string? ProductName,
    string Language,
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
