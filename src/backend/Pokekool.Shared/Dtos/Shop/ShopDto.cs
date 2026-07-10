namespace Pokekool.Shared.Dtos.Shop;

public sealed record ShopDto(
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    string? City,
    string? ZipCode,
    string? GoogleMapsUrl,
    string? HoursJson,
    IReadOnlyList<string> Services,
    string? InstagramUrl,
    string? TiktokUrl,
    IReadOnlyList<string> PhotoUrls,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
