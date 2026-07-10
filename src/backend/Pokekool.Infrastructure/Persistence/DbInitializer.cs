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
///   - utilisateur client de développement (Development/Local uniquement)
/// Idempotent (relance OK).
/// </summary>
public sealed class DbInitializer
{
    private const string DevAdminEmail    = "admin@pokekool.dev";
    private const string DevAdminPassword = "DevAdmin#2026";
    private const string DevClientEmail   = "client@pokekool.dev";
    private const string DevClientPassword = "DevClient#2026";

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
        await SeedDevClientUserAsync(systemRoles, ct);
    }

    // ============================================================
    // PERMISSIONS CATALOG
    // ============================================================
    private async Task SeedPermissionsCatalogAsync(CancellationToken ct)
    {
        var existing = await _db.Permissions.ToDictionaryAsync(p => p.Code, ct);

        var added = 0;
        var updated = 0;
        foreach (var code in Permissions.All)
        {
            var desc = DescribePermission(code);
            if (existing.TryGetValue(code, out var perm))
            {
                if (perm.Description != desc) { perm.UpdateDescription(desc); updated++; }
            }
            else
            {
                await _db.Permissions.AddAsync(Permission.Create(code, Permissions.CategoryOf(code), desc), ct);
                added++;
            }
        }

        if (added > 0 || updated > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Permissions catalog: {Added} ajoutées, {Updated} mises à jour.", added, updated);
        }
        else
        {
            _logger.LogDebug("Permissions catalog already up to date.");
        }
    }

    private static string DescribePermission(string code) => code switch
    {
        Permissions.UsersRead          => "Voir la liste des membres et leurs informations",
        Permissions.UsersWrite         => "Ajouter de nouveaux membres et modifier leurs informations",
        Permissions.UsersDelete        => "Retirer définitivement un membre de la plateforme",
        Permissions.UsersResetPassword => "Envoyer un lien de réinitialisation de mot de passe à un membre",
        Permissions.UsersAssignRoles   => "Changer le rôle attribué à un membre",
        Permissions.RolesRead          => "Voir les rôles existants et leurs permissions",
        Permissions.RolesWrite         => "Créer de nouveaux rôles et modifier ceux qui existent",
        Permissions.RolesDelete        => "Supprimer un rôle personnalisé",
        Permissions.CardsRead          => "Parcourir le catalogue de cartes et sets Pokémon",
        Permissions.CardsSync          => "Lancer une mise à jour du catalogue depuis la source officielle",
        Permissions.ShopRead           => "Consulter le présentoir, le stock et le planning des rendez-vous",
        Permissions.ShopWrite          => "Gérer les articles, le stock et les rendez-vous de la boutique",
        Permissions.ShopManage         => "Modifier les informations de la boutique (adresse, horaires, services)",
        Permissions.StorageRead        => "Consulter les salles, cartons, classeurs et la vue 3D du stockage physique",
        Permissions.StorageWrite       => "Gérer les salles, cartons, classeurs, localisations et modèles 3D du stockage",
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

        // Migration : renommer l'ancien rôle "user" → "client"
        if (existing.TryGetValue("user", out var legacyUserRole) && !existing.ContainsKey(SystemRoles.Client))
        {
            legacyUserRole.SeedRename(SystemRoles.Client, "Client", "Rôle par défaut des clients Pokekool.");
            await _db.SaveChangesAsync(ct);
            existing.Remove("user");
            existing[SystemRoles.Client] = legacyUserRole;
            _logger.LogInformation("System role 'user' renamed to 'client'.");
        }

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
             "Rôle plateforme — contourne toutes les vérifications de permissions.",
             (IReadOnlyList<string>)Array.Empty<string>()),

            (SystemRoles.Admin,
             "Admin",
             "Accès complet à toutes les fonctionnalités de Pokekool.",
             (IReadOnlyList<string>)Permissions.All.ToArray()),

            (SystemRoles.Client,
             "Client",
             "Rôle par défaut des clients Pokekool.",
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

    // ============================================================
    // DEV CLIENT USER
    // ============================================================
    private async Task SeedDevClientUserAsync(Dictionary<string, Role> systemRoles, CancellationToken ct)
    {
        var email  = Email.Create(DevClientEmail);
        var exists = await _db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, ct);

        if (exists)
        {
            _logger.LogDebug("Dev client user '{Email}' already exists, skipping seed.", DevClientEmail);
            return;
        }

        if (!systemRoles.TryGetValue(SystemRoles.Client, out var clientRole))
            throw new InvalidOperationException("System role 'client' is missing — seed inconsistent.");

        var user = User.Create(
            email:        email,
            passwordHash: _hasher.Hash(DevClientPassword),
            firstName:    "Dev",
            lastName:     "Client");

        user.AssignRole(clientRole.Id);

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Dev client user seeded. Email: {Email} | Password: {Password}",
            DevClientEmail, DevClientPassword);
    }
}
