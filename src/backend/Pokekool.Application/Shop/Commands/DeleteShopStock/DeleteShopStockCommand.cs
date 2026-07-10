using MediatR;

namespace Pokekool.Application.Shop.Commands.DeleteShopStock;

public sealed record DeleteShopStockCommand(Guid Id) : IRequest<Unit>;
