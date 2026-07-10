using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.ToggleShopProductAvailability;

public sealed record ToggleShopProductAvailabilityCommand(Guid Id, bool IsAvailable)
    : IRequest<ShopProductDto>;
