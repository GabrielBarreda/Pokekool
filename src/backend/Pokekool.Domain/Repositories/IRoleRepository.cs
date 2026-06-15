using Pokekool.Domain.Entities.Authorization;

namespace Pokekool.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère un rôle visible par le tenant courant : rôle système OU rôle du tenant.
    /// </summary>
    Task<Role?> GetByCodeAsync(Guid? tenantId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liste tous les rôles visibles pour un tenant : rôles système + rôles propres du tenant.
    /// </summary>
    Task<IReadOnlyList<Role>> ListVisibleAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Tous les rôles système (utilisé au seed et par le résolveur de permissions).</summary>
    Task<IReadOnlyList<Role>> ListSystemRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Récupère plusieurs rôles par leurs identifiants en une requête.</summary>
    Task<IReadOnlyList<Role>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    void Remove(Role role);
}
