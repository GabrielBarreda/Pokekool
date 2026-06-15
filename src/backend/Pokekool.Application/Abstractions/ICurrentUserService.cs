namespace Pokekool.Application.Abstractions;

/// <summary>
/// Expose l'utilisateur courant à partir du contexte HTTP (claims du JWT).
/// Implémenté dans Infrastructure (lit IHttpContextAccessor).
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
}
