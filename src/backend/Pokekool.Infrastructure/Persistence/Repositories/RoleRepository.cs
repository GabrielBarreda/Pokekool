using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly PokekoolDbContext _db;

    public RoleRepository(PokekoolDbContext db) => _db = db;

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByCodeAsync(Guid? tenantId, string code, CancellationToken cancellationToken = default)
    {
        var c = code.ToLowerInvariant();
        return _db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(
                r => r.Code == c && (r.TenantId == null || r.TenantId == tenantId),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> ListVisibleAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _db.Roles
            .Include(r => r.Permissions)
            .Where(r => r.TenantId == null || r.TenantId == tenantId)
            .OrderBy(r => r.IsSystem ? 0 : 1).ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> ListSystemRolesAsync(CancellationToken cancellationToken = default)
        => await _db.Roles
            .Include(r => r.Permissions)
            .Where(r => r.IsSystem)
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        return await _db.Roles
            .Include(r => r.Permissions)
            .Where(r => idSet.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        => await _db.Roles.AddAsync(role, cancellationToken);

    public void Remove(Role role) => _db.Roles.Remove(role);
}
