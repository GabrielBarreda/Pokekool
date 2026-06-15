using Pokekool.Domain.Entities.Authorization;
using Pokekool.Shared.Dtos.Authorization;

namespace Pokekool.Application.Authorization;

internal static class RoleMappers
{
    public static RoleDto ToDto(this Role role) => new(
        role.Id,
        role.TenantId,
        role.Code,
        role.Name,
        role.Description,
        role.IsSystem,
        role.PermissionCodes.OrderBy(c => c).ToList(),
        role.CreatedAt,
        role.UpdatedAt);
}
