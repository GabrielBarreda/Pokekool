using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Authorization;

/// <summary>
/// Rôle = bouquet de permissions. Système (tenant_id NULL, is_system=true)
/// ou custom par tenant (tenant_id NOT NULL, is_system=false).
///
/// Le SuperAdmin n'a pas d'entrée Role : il bypass la résolution de permissions
/// au niveau du PermissionResolver.
/// </summary>
public sealed class Role : Entity
{
    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    /// <summary>Vue plate des codes pour les usages applicatifs.</summary>
    public IReadOnlyCollection<string> PermissionCodes
        => _permissions.Select(p => p.PermissionCode).ToList().AsReadOnly();

    private Role() { }

    public static Role CreateSystemRole(string code, string name, string? description = null)
    {
        ValidateCommon(code, name);
        return new Role
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSystem = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Role CreateCustomRole(Guid tenantId, string code, string name, string? description = null)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException("role.tenant", "TenantId is required for custom roles");
        ValidateCommon(code, name);
        return new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Rename(string name)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("role.name", "Role name is required");
        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        EnsureMutable();
        Description = description?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddPermission(string code)
    {
        EnsureMutable();
        AddPermissionInternal(code);
    }

    public void RemovePermission(string code)
    {
        EnsureMutable();
        _permissions.RemoveAll(p => p.PermissionCode == code);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReplacePermissions(IEnumerable<string> codes)
    {
        EnsureMutable();
        _permissions.Clear();
        foreach (var c in codes ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(c))
                _permissions.Add(new RolePermission(Id, c.Trim()));
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Renomme le code/nom/description sans le verrou IsSystem (réservé au seed).</summary>
    public void SeedRename(string code, string name, string? description)
    {
        Code        = code.Trim().ToLowerInvariant();
        Name        = name.Trim();
        Description = description?.Trim();
        UpdatedAt   = DateTimeOffset.UtcNow;
    }

    /// <summary>Pose les permissions sans le verrou IsSystem (réservé au seed).</summary>
    public void SeedPermissions(IEnumerable<string> codes)
    {
        _permissions.Clear();
        foreach (var c in codes ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(c))
                _permissions.Add(new RolePermission(Id, c.Trim()));
        }
    }

    private void AddPermissionInternal(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("permission.code", "Permission code is required");
        if (_permissions.Any(p => p.PermissionCode == code)) return;
        _permissions.Add(new RolePermission(Id, code.Trim()));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureMutable()
    {
        if (IsSystem)
            throw new DomainException("role.is_system", "System roles cannot be modified.");
    }

    private static void ValidateCommon(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("role.code", "Role code is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("role.name", "Role name is required");
    }
}

/// <summary>
/// Lien Rôle → Permission. Matérialise la table role_permissions.
/// Propriété de l'agrégat Role : ne se crée qu'à travers Role.AddPermission().
/// </summary>
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public string PermissionCode { get; private set; } = default!;

    private RolePermission() { }

    internal RolePermission(Guid roleId, string permissionCode)
    {
        RoleId = roleId;
        PermissionCode = permissionCode;
    }
}
