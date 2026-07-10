using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateAppointment;

internal sealed class UpdateAppointmentCommandHandler
    : IRequestHandler<UpdateAppointmentCommand, ShopAppointmentDto>
{
    private readonly IShopAppointmentRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateAppointmentCommandHandler(IShopAppointmentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ShopAppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.AppointmentNotFound(request.Id);

        appt.UpdateDetails(
            request.ScheduledAt, request.DurationMinutes,
            ShopEnumMap.ParseServiceType(request.ServiceType),
            request.NotesInternal, request.AssignedToUserId);

        await _uow.SaveChangesAsync(ct);
        return ShopMapper.ToDto(appt);
    }
}
