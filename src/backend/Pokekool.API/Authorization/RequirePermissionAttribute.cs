using Microsoft.AspNetCore.Authorization;

namespace Pokekool.API.Authorization;

/// <summary>
/// Marque un controller/action comme nécessitant une permission précise.
/// Usage: <c>[RequirePermission(Permissions.UsersWrite)]</c>.
///
/// La chaîne devient le nom de policy ASP.NET (préfixée pour éviter les collisions),
/// résolue dynamiquement par <see cref="PermissionPolicyProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public RequirePermissionAttribute(string permission)
        => Policy = $"{PolicyPrefix}{permission}";
}
