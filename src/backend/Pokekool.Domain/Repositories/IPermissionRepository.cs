using Pokekool.Domain.Entities.Authorization;

namespace Pokekool.Domain.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);
}
