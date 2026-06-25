using Pokekool.Domain.Entities;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users;

internal static class UserMappers
{
    public static UserSummaryDto ToSummaryDto(this User user, IReadOnlyDictionary<Guid, Role> rolesById)
    {
        var roleCodes = user.RoleAssignments
            .Select(ra => ra.RoleId)
            .Distinct()
            .Where(rolesById.ContainsKey)
            .Select(id => rolesById[id].Code)
            .ToList();

        return new UserSummaryDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.IsActive,
            roleCodes,
            HasPendingMessages: false,   // branché quand la table Message existera
            user.CreatedAt,
            user.LastLoginAt);
    }

    public static UserDto ToDto(this User user, IReadOnlyDictionary<Guid, Role> rolesById)
    {
        var roleIds = user.RoleAssignments.Select(ra => ra.RoleId).Distinct().ToList();
        var roleCodes = roleIds
            .Where(rolesById.ContainsKey)
            .Select(id => rolesById[id].Code)
            .Distinct()
            .ToList();

        return new UserDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.IsActive,
            user.EmailConfirmed,
            user.LastLoginAt,
            roleIds,
            roleCodes,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
