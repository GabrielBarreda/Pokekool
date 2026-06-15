using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Queries;

public sealed record ListUsersQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<UserDto>>;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public ListUsersQueryHandler(IUserRepository users, IRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _users.ListAsync(request.IncludeInactive, cancellationToken);

        var roleIds = users.SelectMany(u => u.RoleAssignments).Select(ra => ra.RoleId).Distinct().ToList();
        var roles = (await _roles.GetManyByIdsAsync(roleIds, cancellationToken)).ToDictionary(r => r.Id);

        return users.Select(u => u.ToDto(roles)).ToList();
    }
}
