using Microsoft.AspNetCore.Authorization;

namespace Pokekool.API.Authorization;

/// <summary>
/// Requirement Authorization ASP.NET porté par <see cref="RequirePermissionAttribute"/>.
/// Évalué par <see cref="PermissionAuthorizationHandler"/>.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission) => Permission = permission;
}
