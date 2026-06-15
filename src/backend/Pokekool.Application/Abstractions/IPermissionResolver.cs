namespace Pokekool.Application.Abstractions;

/// <summary>
/// Résout les permissions effectives du user courant.
///
/// Lit les claims du JWT (rôles + super_admin bypass), puis pour chaque rôle
/// charge le bouquet de permissions associé (avec cache Redis 5 min).
/// </summary>
public interface IPermissionResolver
{
    /// <summary>True si le user courant possède la permission demandée.</summary>
    Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>Liste les codes de permissions effectivement attribués au user courant.</summary>
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalide le cache d'un rôle (à appeler quand un rôle est modifié/supprimé,
    /// ou quand on attribue/retire un rôle à un user).
    /// </summary>
    Task InvalidateRoleCacheAsync(Guid roleId, CancellationToken cancellationToken = default);
}
