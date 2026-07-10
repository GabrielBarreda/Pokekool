using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateShopInfo;

public sealed record UpdateShopInfoCommand(
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
    string[]? PhotoUrls) : IRequest<ShopDto>;
