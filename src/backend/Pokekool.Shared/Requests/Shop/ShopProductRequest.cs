namespace Pokekool.Shared.Requests.Shop;

/// <summary>Corps de création / modification d'un article du présentoir.</summary>
public sealed record ShopProductRequest(
    string Type,
    Guid? CardId,
    string? ProductName,
    string? Condition,
    string? GradingCompany,
    decimal? GradeValue,
    decimal Price,
    string? Currency,
    int Quantity,
    bool IsAvailable,
    string? Description,
    string? PhotoUrl);

public sealed record ToggleAvailabilityRequest(bool IsAvailable);
