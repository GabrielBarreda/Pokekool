using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateAppointmentStatus;

internal sealed class UpdateAppointmentStatusCommandHandler
    : IRequestHandler<UpdateAppointmentStatusCommand, ShopAppointmentDto>
{
    private readonly IShopAppointmentRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateAppointmentStatusCommandHandler(IShopAppointmentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ShopAppointmentDto> Handle(UpdateAppointmentStatusCommand request, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.AppointmentNotFound(request.Id);

        appt.ChangeStatus(ShopEnumMap.ParseStatus(request.Status));
        await _uow.SaveChangesAsync(ct);
        return ShopMapper.ToDto(appt);
    }
}
