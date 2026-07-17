using System.Text;
using System.Text.Json;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Mobile.Services;

/// <summary>
/// Détient la session courante : access token (mémoire), utilisateur, rôles décodés du JWT.
/// Pilote le choix du TabBar (Admin vs Client) dans AppShell.
/// </summary>
public sealed class SessionService
{
    public string? AccessToken { get; private set; }
    public DateTimeOffset AccessTokenExpiresAt { get; private set; }
    public UserSummaryDto? User { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = Array.Empty<string>();

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    /// <summary>admin / super_admin / employee → interface Admin ; sinon interface Client.</summary>
    public bool IsAdmin =>
        Roles.Contains(SystemRoles.Admin) ||
        Roles.Contains(SystemRoles.SuperAdmin) ||
        Roles.Contains("employee");

    public event EventHandler? SessionChanged;

    public void SetSession(LoginResponse response)
    {
        AccessToken = response.AccessToken;
        AccessTokenExpiresAt = response.AccessTokenExpiresAt;
        User = response.User;
        Roles = response.User?.Roles is { Count: > 0 }
            ? response.User.Roles
            : DecodeRolesFromJwt(response.AccessToken);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        AccessToken = null;
        AccessTokenExpiresAt = default;
        User = null;
        Roles = Array.Empty<string>();
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Décode le payload du JWT (sans validation — la validation est côté serveur).</summary>
    internal static IReadOnlyList<string> DecodeRolesFromJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return Array.Empty<string>();

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

            if (!doc.RootElement.TryGetProperty(ClaimTypes.Roles, out var rolesEl))
                return Array.Empty<string>();

            return rolesEl.ValueKind switch
            {
                JsonValueKind.Array => rolesEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToArray(),
                JsonValueKind.String => new[] { rolesEl.GetString()! },
                _ => Array.Empty<string>(),
            };
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
