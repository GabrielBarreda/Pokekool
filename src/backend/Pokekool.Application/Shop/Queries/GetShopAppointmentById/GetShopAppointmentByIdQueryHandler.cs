using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopAppointmentById;

internal sealed class GetShopAppointmentByIdQueryHandler
    : IRequestHandler<GetShopAppointmentByIdQuery, ShopAppointmentDto>
{
    private readonly IShopAppointmentRepository _repo;

    public GetShopAppointmentByIdQueryHandler(IShopAppointmentRepository repo) => _repo = repo;

    public async Task<ShopAppointmentDto> Handle(GetShopAppointmentByIdQuery request, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.AppointmentNotFound(request.Id);
        return ShopMapper.ToDto(appt);
    }
}
