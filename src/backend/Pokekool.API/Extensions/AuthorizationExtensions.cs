using Microsoft.AspNetCore.Authorization;
using Pokekool.API.Authorization;

namespace Pokekool.API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPokekoolAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
