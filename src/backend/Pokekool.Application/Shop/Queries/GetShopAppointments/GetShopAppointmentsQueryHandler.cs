using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopAppointments;

internal sealed class GetShopAppointmentsQueryHandler
    : IRequestHandler<GetShopAppointmentsQuery, PagedResult<ShopAppointmentDto>>
{
    private readonly IShopAppointmentRepository _repo;

    public GetShopAppointmentsQueryHandler(IShopAppointmentRepository repo) => _repo = repo;

    public async Task<PagedResult<ShopAppointmentDto>> Handle(GetShopAppointmentsQuery request, CancellationToken ct)
    {
        var status = ShopEnumMap.ParseStatusOrNull(request.Status);
        var service = ShopEnumMap.ParseServiceTypeOrNull(request.ServiceType);

        var (items, total) = await _repo.SearchAsync(
            status, service, request.From, request.To, request.Page, request.PageSize, ct);

        var dtos = items.Select(ShopMapper.ToDto).ToList();
        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PagedResult<ShopAppointmentDto>(dtos, request.Page, request.PageSize, total, totalPages);
    }
}
