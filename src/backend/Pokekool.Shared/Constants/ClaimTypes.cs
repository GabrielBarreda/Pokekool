namespace Pokekool.Shared.Constants;

/// <summary>
/// JWT claim names used by the Pokekool platform. Single source of truth.
/// </summary>
public static class ClaimTypes
{
    public const string UserId = "sub";
    public const string Email  = "email";
    public const string Roles  = "roles";
}
