using Pokekool.Domain.Common;
using Pokekool.Domain.ValueObjects;

namespace Pokekool.Domain.Entities;

public sealed class User : Entity
{
    public Guid? TenantId { get; private set; }
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool EmailConfirmed { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private readonly List<UserRoleAssignment> _roleAssignments = [];
    public IReadOnlyCollection<UserRoleAssignment> RoleAssignments => _roleAssignments.AsReadOnly();

    private User() { }

    public static User Create(Email email, string passwordHash, string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("user.password", "Password hash is required");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void RecordLogin() => LastLoginAt = DateTimeOffset.UtcNow;

    public void UpdateProfile(string? firstName, string? lastName, string? phone)
    {
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        Phone = phone?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("user.password", "Password hash is required");
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = DateTimeOffset.UtcNow;
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new DomainException("user.role", "RoleId is required");
        if (_roleAssignments.Any(r => r.RoleId == roleId))
            return;
        _roleAssignments.Add(new UserRoleAssignment(Id, roleId));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveRole(Guid roleId)
    {
        var found = _roleAssignments.FirstOrDefault(r => r.RoleId == roleId);
        if (found is not null)
        {
            _roleAssignments.Remove(found);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}

public sealed class UserRoleAssignment
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserRoleAssignment() { }

    internal UserRoleAssignment(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
