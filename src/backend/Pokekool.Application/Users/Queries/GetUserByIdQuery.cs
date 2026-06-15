using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public GetUserByIdQueryHandler(IUserRepository users, IRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.Id, cancellationToken)
            ?? throw UserErrors.UserNotFound(request.Id);
        var roleIds = user.RoleAssignments.Select(ra => ra.RoleId).Distinct().ToList();
        var roles = (await _roles.GetManyByIdsAsync(roleIds, cancellationToken)).ToDictionary(r => r.Id);
        return user.ToDto(roles);
    }
}
