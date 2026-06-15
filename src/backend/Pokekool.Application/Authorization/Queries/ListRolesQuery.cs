using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Authorization;

namespace Pokekool.Application.Authorization.Queries;

public sealed record ListRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleRepository _roles;

    public ListRolesQueryHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roles.ListSystemRolesAsync(cancellationToken);
        return roles.Select(r => r.ToDto()).ToList();
    }
}
