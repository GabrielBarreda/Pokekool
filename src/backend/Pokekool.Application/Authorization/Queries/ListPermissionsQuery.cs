using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Authorization;

namespace Pokekool.Application.Authorization.Queries;

public sealed record ListPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;

public sealed class ListPermissionsQueryHandler : IRequestHandler<ListPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IPermissionRepository _permissions;

    public ListPermissionsQueryHandler(IPermissionRepository permissions) => _permissions = permissions;

    public async Task<IReadOnlyList<PermissionDto>> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
    {
        var perms = await _permissions.ListAllAsync(cancellationToken);
        return perms.Select(p => new PermissionDto(p.Code, p.Category, p.Description)).ToList();
    }
}
