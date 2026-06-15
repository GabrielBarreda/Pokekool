using Pokekool.Domain.Common;

namespace Pokekool.Application.Users;

public static class UserErrors
{
    public static DomainException UserNotFound(Guid id)
        => new("user.not_found", $"User '{id}' not found.");
    public static DomainException EmailAlreadyTaken(string email)
        => new("user.email_taken", $"Email '{email}' is already taken.");
    public static DomainException CannotActOnSelf()
        => new("user.self_action", "You cannot perform this action on your own account.");
    public static DomainException LastActiveAdmin()
        => new("user.last_admin", "Cannot deactivate or delete the last active administrator.");
    public static DomainException InvalidCurrentPassword()
        => new("user.invalid_current_password", "Current password is incorrect.");
    public static DomainException RoleNotFound(Guid id)
        => new("role.not_found", $"Role '{id}' not found.");
    public static DomainException CannotAssignSuperAdmin()
        => new("user.super_admin_forbidden", "The SuperAdmin role cannot be assigned directly.");
}
