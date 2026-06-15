using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly PokekoolDbContext _db;

    public PermissionRepository(PokekoolDbContext db) => _db = db;

    public async Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken cancellationToken = default)
        => await _db.Permissions
            .OrderBy(p => p.Category).ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default)
        => _db.Permissions.AnyAsync(p => p.Code == code, cancellationToken);

    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
        => await _db.Permissions.AddAsync(permission, cancellationToken);
}
