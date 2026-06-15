using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Application.Users.Queries;

public sealed record GetMeQuery : IRequest<CurrentUserDto>;

public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, CurrentUserDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _users;
    private readonly IPermissionResolver _permissionResolver;

    public GetMeQueryHandler(
        ICurrentUserService currentUser,
        IUserRepository users,
        IPermissionResolver permissionResolver)
    {
        _currentUser = currentUser;
        _users = users;
        _permissionResolver = permissionResolver;
    }

    public async Task<CurrentUserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var user = userId.HasValue ? await _users.GetByIdAsync(userId.Value, cancellationToken) : null;
        var permissions = await _permissionResolver.GetPermissionsAsync(cancellationToken);

        return new CurrentUserDto(
            userId,
            _currentUser.Email,
            null,
            _currentUser.Roles,
            _currentUser.IsAuthenticated,
            user?.FirstName ?? "",
            user?.LastName ?? "",
            permissions
        );
    }
}
