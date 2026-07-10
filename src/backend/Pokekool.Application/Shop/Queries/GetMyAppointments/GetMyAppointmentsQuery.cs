using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetMyAppointments;

/// <summary>Historique des RDV du client connecté (filtrés par l'email du compte JWT).</summary>
public sealed record GetMyAppointmentsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ShopAppointmentDto>>;

public sealed class GetMyAppointmentsQueryHandler
    : IRequestHandler<GetMyAppointmentsQuery, PagedResult<ShopAppointmentDto>>
{
    private readonly IShopClientRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyAppointmentsQueryHandler(IShopClientRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ShopAppointmentDto>> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var email = _currentUser.Email
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, total) = await _repo.GetAppointmentsByEmailPagedAsync(email, page, pageSize, cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<ShopAppointmentDto>(items, page, pageSize, total, totalPages);
    }
}
