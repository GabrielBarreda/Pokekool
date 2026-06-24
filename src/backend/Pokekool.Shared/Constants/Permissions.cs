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

    public static IReadOnlyList<string> All => new[]
    {
        UsersRead, UsersWrite, UsersDelete, UsersResetPassword, UsersAssignRoles,
        RolesRead, RolesWrite, RolesDelete,
        CardsRead, CardsSync,
        SettingsRead, SettingsWrite,
    };

    public static string CategoryOf(string code) => code.Split(':')[0];
}
