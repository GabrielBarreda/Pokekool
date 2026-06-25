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

    public async Task<(IReadOnlyList<User> Items, long TotalCount)> GetPagedAsync(
        string? search,
        Guid? roleId,
        bool? recentOnly,
        string? sortBy,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Users
            .Include(u => u.RoleAssignments)
            .AsQueryable();

        // ── Filtres ──────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.Like(EF.Property<string>(u, "email"), pattern) ||
                (u.FirstName != null && EF.Functions.Like(u.FirstName, pattern)) ||
                (u.LastName  != null && EF.Functions.Like(u.LastName,  pattern)));
        }

        if (roleId.HasValue)
            query = query.Where(u => u.RoleAssignments.Any(ra => ra.RoleId == roleId.Value));

        if (recentOnly == true)
        {
            var since = DateTimeOffset.UtcNow.AddDays(-7);
            query = query.Where(u => u.CreatedAt >= since);
        }

        // ── Count avant pagination ───────────────────────────────────────────
        var total = await query.LongCountAsync(ct);

        // ── Tri ─────────────────────────────────────────────────────────────
        query = sortBy switch
        {
            "name_asc"     => query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
            "name_desc"    => query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName),
            "email"        => query.OrderBy(u => EF.Property<string>(u, "email")),
            _              => query.OrderByDescending(u => u.CreatedAt), // "created_desc" (default)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);
}
