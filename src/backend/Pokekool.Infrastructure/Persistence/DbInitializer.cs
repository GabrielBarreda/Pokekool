using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Domain.ValueObjects;
using Pokekool.Shared.Constants;

namespace Pokekool.Infrastructure.Persistence;

/// <summary>
/// Applique les migrations EF Core puis seede :
///   - catalogue des permissions
///   - rôles système avec leurs permissions par défaut
///   - utilisateur admin de développement
/// Idempotent (relance OK).
/// </summary>
public sealed class DbInitializer
{
    private const string DevAdminEmail    = "admin@pokekool.dev";
    private const string DevAdminPassword = "DevAdmin#2026";

    private readonly PokekoolDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(PokekoolDbContext db, IPasswordHasher hasher, ILogger<DbInitializer> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Applying EF Core migrations...");
        await _db.Database.MigrateAsync(ct);

        await SeedPermissionsCatalogAsync(ct);
        var systemRoles = await SeedSystemRolesAsync(ct);
        await SeedDevAdminUserAsync(systemRoles, ct);
    }

    // ============================================================
    // PERMISSIONS CATALOG
    // ============================================================
    private async Task SeedPermissionsCatalogAsync(CancellationToken ct)
    {
        var existingCodes = await _db.Permissions.Select(p => p.Code).ToListAsync(ct);
        var existing = existingCodes.ToHashSet();

        var added = 0;
        foreach (var code in Permissions.All)
        {
            if (existing.Contains(code)) continue;
            var perm = Permission.Create(code, Permissions.CategoryOf(code), DescribePermission(code));
            await _db.Permissions.AddAsync(perm, ct);
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Permissions catalog seeded: {Count} new permissions.", added);
        }
        else
        {
            _logger.LogDebug("Permissions catalog already up to date.");
        }
    }

    private static string DescribePermission(string code) => code switch
    {
        Permissions.UsersRead          => "View users",
        Permissions.UsersWrite         => "Create / update users",
        Permissions.UsersDelete        => "Delete users",
        Permissions.UsersResetPassword => "Reset a user password",
        Permissions.UsersAssignRoles   => "Assign roles to users",
        Permissions.RolesRead          => "View roles",
        Permissions.RolesWrite         => "Create / update custom roles",
        Permissions.RolesDelete        => "Delete custom roles",
        Permissions.CardsRead          => "View Pokemon cards and sets",
        Permissions.CardsSync          => "Trigger synchronization with PokemonTCG API",
        _ => code,
    };

    // ============================================================
    // SYSTEM ROLES
    // ============================================================
    private async Task<Dictionary<string, Role>> SeedSystemRolesAsync(CancellationToken ct)
    {
        var existing = await _db.Roles
            .IgnoreQueryFilters()
            .Include(r => r.Permissions)
            .Where(r => r.IsSystem)
            .ToDictionaryAsync(r => r.Code, ct);

        var defaults = SystemRoleBlueprints();
        var addedOrUpdated = 0;

        foreach (var (code, name, description, permissions) in defaults)
        {
            if (existing.TryGetValue(code, out var role))
            {
                var current = role.PermissionCodes.OrderBy(c => c).ToList();
                var target  = permissions.OrderBy(c => c).ToList();
                if (!current.SequenceEqual(target))
                {
                    role.SeedPermissions(permissions);
                    addedOrUpdated++;
                }
            }
            else
            {
                var newRole = Role.CreateSystemRole(code, name, description);
                newRole.SeedPermissions(permissions);
                await _db.Roles.AddAsync(newRole, ct);
                existing[code] = newRole;
                addedOrUpdated++;
            }
        }

        if (addedOrUpdated > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("System roles seeded/updated: {Count}.", addedOrUpdated);
        }
        else
        {
            _logger.LogDebug("System roles already up to date.");
        }

        return existing;
    }

    private static IReadOnlyList<(string Code, string Name, string Description, IReadOnlyList<string> Permissions)>
        SystemRoleBlueprints() => new[]
        {
            (SystemRoles.SuperAdmin,
             "Super Admin",
             "Platform role — bypasses all permission checks.",
             (IReadOnlyList<string>)Array.Empty<string>()),

            (SystemRoles.Admin,
             "Admin",
             "Full access to all Pokekool features.",
             (IReadOnlyList<string>)Permissions.All.ToArray()),

            (SystemRoles.User,
             "User",
             "Standard user — read access.",
             (IReadOnlyList<string>)new[]
             {
                 Permissions.UsersRead,
                 Permissions.RolesRead,
                 Permissions.CardsRead,
             }),
        };

    // ============================================================
    // DEV ADMIN USER
    // ============================================================
    private async Task SeedDevAdminUserAsync(Dictionary<string, Role> systemRoles, CancellationToken ct)
    {
        var email  = Email.Create(DevAdminEmail);
        var exists = await _db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, ct);

        if (exists)
        {
            _logger.LogDebug("Dev admin user '{Email}' already exists, skipping seed.", DevAdminEmail);
            return;
        }

        if (!systemRoles.TryGetValue(SystemRoles.Admin, out var adminRole))
            throw new InvalidOperationException("System role 'admin' is missing — seed inconsistent.");

        var user = User.Create(
            email:        email,
            passwordHash: _hasher.Hash(DevAdminPassword),
            firstName:    "Dev",
            lastName:     "Admin");

        user.AssignRole(adminRole.Id);

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Dev admin user seeded. Email: {Email} | Password: {Password}",
            DevAdminEmail, DevAdminPassword);
    }
}
