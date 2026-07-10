namespace Pokekool.Shared.Requests.Shop;

public sealed record UpdateShopInfoRequest(
    string Name,
    string? Description,
    string? Address,
    string? City,
    string? ZipCode,
    string? GoogleMapsUrl,
    string? HoursJson,
    string[]? Services,
    string? InstagramUrl,
    string? TiktokUrl,
    string[]? PhotoUrls);
