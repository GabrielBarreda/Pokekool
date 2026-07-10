namespace Pokekool.Shared.Constants;

public static class Permissions
{
    public const string UsersRead          = "users:read";
    public const string UsersWrite         = "users:write";
    public const string UsersDelete        = "users:delete";
    public const string UsersResetPassword = "users:reset_password";
    public const string UsersAssignRoles   = "users:assign_roles";

    public const string RolesRead   = "roles:read";
    public const string RolesWrite  = "roles:write";
    public const string RolesDelete = "roles:delete";

    public const string CardsRead = "cards:read";
    public const string CardsSync = "cards:sync";

    public const string SettingsRead  = "settings:read";
    public const string SettingsWrite = "settings:write";

    // ---- Module Boutique ----
    /// <summary>Consulter présentoir, stock et planning RDV (admin + employé).</summary>
    public const string ShopRead   = "shop:read";
    /// <summary>Créer / modifier / supprimer articles, stock et RDV (admin + employé).</summary>
    public const string ShopWrite  = "shop:write";
    /// <summary>Modifier les infos de la boutique — réservé aux admins.</summary>
    public const string ShopManage = "shop:manage";

    // ---- Module Stockage Physique ----
    /// <summary>Consulter salles, cartons, classeurs et vue 3D du stockage physique.</summary>
    public const string StorageRead  = "storage:read";
    /// <summary>Gérer salles, cartons, classeurs, localisations et modèles 3D.</summary>
    public const string StorageWrite = "storage:write";

    public static IReadOnlyList<string> All => new[]
    {
        UsersRead, UsersWrite, UsersDelete, UsersResetPassword, UsersAssignRoles,
        RolesRead, RolesWrite, RolesDelete,
        CardsRead, CardsSync,
        SettingsRead, SettingsWrite,
        ShopRead, ShopWrite, ShopManage,
        StorageRead, StorageWrite,
    };

    public static string CategoryOf(string code) => code.Split(':')[0];
}
