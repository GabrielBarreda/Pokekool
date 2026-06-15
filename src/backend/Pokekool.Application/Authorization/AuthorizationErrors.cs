using Pokekool.Domain.Common;

namespace Pokekool.Application.Authorization;

public static class AuthorizationErrors
{
    public static DomainException RoleNotFound(Guid id)
        => new("role.not_found", $"Role '{id}' not found.");
    public static DomainException SystemRoleImmutable()
        => new("role.is_system", "System roles cannot be modified.");
    public static DomainException CannotEscalatePrivileges(IEnumerable<string> missing)
        => new("role.privilege_escalation",
            $"You cannot assign permissions you do not have yourself: {string.Join(", ", missing)}");
    public static DomainException InvalidPermissionCode(string code)
        => new("permission.invalid_code", $"Unknown permission code: {code}");
    public static DomainException RoleCodeAlreadyExists(string code)
        => new("role.code_conflict", $"A role with code '{code}' already exists.");
}
