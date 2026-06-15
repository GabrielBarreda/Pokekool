using Microsoft.AspNetCore.Authorization;
using Pokekool.Application.Abstractions;

namespace Pokekool.API.Authorization;

/// <summary>
/// Évalue les <see cref="PermissionRequirement"/> en s'appuyant sur le résolveur de permissions.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionResolver _resolver;

    public PermissionAuthorizationHandler(IPermissionResolver resolver) => _resolver = resolver;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true) return;

        if (await _resolver.HasPermissionAsync(requirement.Permission))
            context.Succeed(requirement);
    }
}
