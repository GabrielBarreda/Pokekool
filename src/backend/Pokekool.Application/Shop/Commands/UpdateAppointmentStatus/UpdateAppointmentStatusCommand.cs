using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateAppointmentStatus;

public sealed record UpdateAppointmentStatusCommand(Guid Id, string Status)
    : IRequest<ShopAppointmentDto>;
