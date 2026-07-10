using MediatR;

namespace Pokekool.Application.Shop.Commands.DeleteShopProduct;

public sealed record DeleteShopProductCommand(Guid Id) : IRequest<Unit>;
