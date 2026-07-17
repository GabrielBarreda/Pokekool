using System.Net.Http.Json;
using System.Text.Json;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Mobile.Services;

/// <summary>
/// Login / refresh / logout. Access token en mémoire (SessionService),
/// refresh token en SecureStorage (équivalent mobile du cookie HttpOnly web).
/// Utilise son propre HttpClient SANS AuthHttpMessageHandler pour éviter toute récursion sur le refresh.
/// </summary>
public sealed class AuthService
{
    private const string RefreshTokenKey = "pokekool_refresh_token";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SessionService _session;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(SessionService session)
    {
        _session = session;
        _http = new HttpClient { BaseAddress = new Uri(ApiConfig.BaseUrl) };
    }

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"{ApiConfig.V1}auth/login", new LoginRequest(email, password), Json, ct);
        response.EnsureSuccessStatusCode();

        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(Json, ct))!;
        await StoreSessionAsync(login);
        return login;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{ApiConfig.V1}auth/register", request, Json, ct);
        response.EnsureSuccessStatusCode();

        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(Json, ct))!;
        await StoreSessionAsync(login);
        return login;
    }

    /// <summary>Tente de restaurer la session au démarrage à partir du refresh token stocké.</summary>
    public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
        => await TryRefreshAsync(ct);

    /// <summary>Rafraîchit l'access token. Thread-safe (un seul refresh à la fois).</summary>
    public async Task<bool> TryRefreshAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var response = await _http.PostAsJsonAsync(
                $"{ApiConfig.V1}auth/refresh", new RefreshRequest(refreshToken), Json, ct);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                    or System.Net.HttpStatusCode.Forbidden)
                {
                    SecureStorage.Default.Remove(RefreshTokenKey);
                    _session.Clear();
                }
                return false;
            }

            var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(Json, ct))!;
            await StoreSessionAsync(login);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            if (!string.IsNullOrEmpty(refreshToken))
                await _http.PostAsJsonAsync($"{ApiConfig.V1}auth/logout", new RefreshRequest(refreshToken), Json, ct);
        }
        catch
        {
            // Best effort — la session locale est nettoyée quoi qu'il arrive.
        }
        finally
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
            _session.Clear();
        }
    }

    private async Task StoreSessionAsync(LoginResponse login)
    {
        _session.SetSession(login);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, login.RefreshToken);
    }
}
