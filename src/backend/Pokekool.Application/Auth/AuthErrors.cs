using Pokekool.Domain.Common;

namespace Pokekool.Application.Auth;

/// <summary>
/// Erreurs métier d'authentification. Mappées en 401/422 par le middleware d'exceptions.
/// </summary>
public static class AuthErrors
{
    public static DomainException InvalidCredentials()
        => new("auth.invalid_credentials", "Email or password is invalid.");

    public static DomainException AccountDisabled()
        => new("auth.account_disabled", "This account is disabled.");

    public static DomainException InvalidRefreshToken()
        => new("auth.invalid_refresh_token", "The refresh token is invalid or expired.");
}
