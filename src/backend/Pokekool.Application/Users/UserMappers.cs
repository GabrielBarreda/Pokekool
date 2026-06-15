using Pokekool.Domain.Entities;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users;

internal static class UserMappers
{
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
