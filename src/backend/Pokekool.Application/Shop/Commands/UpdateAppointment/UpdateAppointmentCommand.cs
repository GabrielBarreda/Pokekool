using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateAppointment;

public sealed record UpdateAppointmentCommand(
    Guid Id,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string ServiceType,
    string? NotesInternal,
    Guid? AssignedToUserId) : IRequest<ShopAppointmentDto>;
