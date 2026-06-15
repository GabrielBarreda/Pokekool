using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Authorization;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Commands;

public sealed record AssignRolesCommand(Guid UserId, AssignRolesRequest Request) : IRequest<UserDto>;

public sealed class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPermissionResolver _resolver;
    private readonly IUnitOfWork _uow;

    public AssignRolesCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPermissionResolver resolver,
        IUnitOfWork uow)
    {
        _users = users;
        _roles = roles;
        _resolver = resolver;
        _uow = uow;
    }

    public async Task<UserDto> Handle(AssignRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw UserErrors.UserNotFound(request.UserId);

        var newRoleIds = request.Request.RoleIds.Distinct().ToList();
        var newRoles = await _roles.GetManyByIdsAsync(newRoleIds, cancellationToken);
        if (newRoles.Count != newRoleIds.Count)
        {
            var missing = newRoleIds.Except(newRoles.Select(r => r.Id)).First();
            throw UserErrors.RoleNotFound(missing);
        }
        if (newRoles.Any(r => r.Code == SystemRoles.SuperAdmin))
            throw UserErrors.CannotAssignSuperAdmin();

        var mine = await _resolver.GetPermissionsAsync(cancellationToken);
        var requested = newRoles.SelectMany(r => r.PermissionCodes).Distinct();
        var missingPerms = requested.Where(p => !mine.Contains(p)).ToList();
        if (missingPerms.Count > 0)
            throw AuthorizationErrors.CannotEscalatePrivileges(missingPerms);

        var currentIds = user.RoleAssignments.Select(ra => ra.RoleId).Distinct().ToList();
        foreach (var idToRemove in currentIds.Except(newRoleIds)) user.RemoveRole(idToRemove);
        foreach (var idToAdd in newRoleIds.Except(currentIds)) user.AssignRole(idToAdd);

        await _uow.SaveChangesAsync(cancellationToken);

        var allRoleIds = user.RoleAssignments.Select(ra => ra.RoleId).Distinct().ToList();
        var roles = (await _roles.GetManyByIdsAsync(allRoleIds, cancellationToken)).ToDictionary(r => r.Id);
        return user.ToDto(roles);
    }
}
