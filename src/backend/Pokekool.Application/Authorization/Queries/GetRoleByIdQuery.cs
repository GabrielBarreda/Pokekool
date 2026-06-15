using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Authorization;

namespace Pokekool.Application.Authorization.Queries;

public sealed record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto>;

public sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly IRoleRepository _roles;

    public GetRoleByIdQueryHandler(IRoleRepository roles) => _roles = roles;

    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(request.Id, cancellationToken)
            ?? throw AuthorizationErrors.RoleNotFound(request.Id);
        return role.ToDto();
    }
}
