using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.ValueObjects;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly PokekoolDbContext _db;

    public UserRepository(PokekoolDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
        => _db.Users.IgnoreQueryFilters()
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> ListAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Users.Include(u => u.RoleAssignments).AsQueryable();
        if (!includeInactive) query = query.Where(u => u.IsActive);
        return await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);
}
