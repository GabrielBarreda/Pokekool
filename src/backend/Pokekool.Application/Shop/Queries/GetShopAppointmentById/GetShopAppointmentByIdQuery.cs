using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopAppointmentById;

public sealed record GetShopAppointmentByIdQuery(Guid Id) : IRequest<ShopAppointmentDto>;
