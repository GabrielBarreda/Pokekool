using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pokekool.Application.Abstractions;

namespace Pokekool.Infrastructure.DataProviders.TcgPlayer;

/// <summary>
/// DelegatingHandler OAuth 2.0 Client Credentials pour TCGPlayer.
///
/// Le token est mis en cache dans IMemoryCache pendant 55 minutes
/// (les tokens TCGPlayer expirent à 1h — on renouvelle avec une marge de 5 min).
/// Les clés API (public_key / private_key) sont lues depuis IProviderSettingsService.
/// </summary>
internal sealed class TcgPlayerTokenHandler : DelegatingHandler
{
    private const string ProviderName = "tcgplayer";
    private const string CacheKey     = "tcgplayer:access_token";

    // Durée de cache = 55 min (token expire à 1h, marge de 5 min)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(55);

    private readonly IProviderSettingsService _settings;
    private readonly IMemoryCache             _cache;
    private readonly TcgPlayerOptions         _opts;
    private readonly ILogger<TcgPlayerTokenHandler> _logger;

    // Verrou pour éviter les requêtes de token concurrentes
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public TcgPlayerTokenHandler(
        IProviderSettingsService settings,
        IMemoryCache cache,
        IOptions<TcgPlayerOptions> opts,
        ILogger<TcgPlayerTokenHandler> logger)
    {
        _settings = settings;
        _cache    = cache;
        _opts     = opts.Value;
        _logger   = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await GetOrRefreshTokenAsync(ct);

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        else
            _logger.LogWarning("TcgPlayerTokenHandler — token manquant, requête envoyée sans auth");

        return await base.SendAsync(request, ct);
    }

    // ── Token management ──────────────────────────────────────────────────────

    private async Task<string?> GetOrRefreshTokenAsync(CancellationToken ct)
    {
        // Fast path sans verrou si le token est déjà en cache
        if (_cache.TryGetValue(CacheKey, out string? cached))
            return cached;

        await _tokenLock.WaitAsync(ct);
        try
        {
            // Double-check après acquisition du verrou
            if (_cache.TryGetValue(CacheKey, out cached))
                return cached;

            return await FetchTokenAsync(ct);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string?> FetchTokenAsync(CancellationToken ct)
    {
        var publicKey  = await _settings.GetAsync(ProviderName, "public_key",  ct);
        var privateKey = await _settings.GetAsync(ProviderName, "private_key", ct);

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            _logger.LogWarning(
                "TcgPlayerTokenHandler — public_key ou private_key non configuré");
            return null;
        }

        using var http = new HttpClient();

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = publicKey,
            ["client_secret"] = privateKey,
        });

        try
        {
            var response = await http.PostAsync(_opts.TokenUrl, body, ct);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content
                .ReadFromJsonAsync<TcgPlayerTokenResponse>(cancellationToken: ct);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogWarning("TcgPlayerTokenHandler — réponse token invalide");
                return null;
            }

            _cache.Set(CacheKey, tokenResponse.AccessToken, CacheDuration);
            _logger.LogInformation(
                "TcgPlayerTokenHandler — token renouvelé, expire dans {Expiry} min", 55);

            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TcgPlayerTokenHandler — échec récupération du token OAuth");
            return null;
        }
    }
}
