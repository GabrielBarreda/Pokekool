using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Authorization;

/// <summary>
/// Permission atomique du catalogue. Référencée par les role_permissions.
/// Identifiant : Code (string), pas un Guid : on raisonne en codes stables côté code/JWT/UI.
/// </summary>
public sealed class Permission
{
    public string Code { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private Permission() { }

    public static Permission Create(string code, string category, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("permission.code", "Permission code is required");
        return new Permission
        {
            Code = code.Trim(),
            Category = category.Trim().ToLowerInvariant(),
            Description = description.Trim()
        };
    }
}
