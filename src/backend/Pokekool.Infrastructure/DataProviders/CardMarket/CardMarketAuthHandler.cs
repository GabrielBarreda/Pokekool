using System.Security.Cryptography;
using System.Text;
using Pokekool.Application.Abstractions;

namespace Pokekool.Infrastructure.DataProviders.CardMarket;

/// <summary>
/// DelegatingHandler qui signe chaque requête CardMarket via OAuth 1.0a (HMAC-SHA1).
/// Les 4 clés sont lues depuis IProviderSettingsService (chiffrées en base).
/// </summary>
internal sealed class CardMarketAuthHandler : DelegatingHandler
{
    private const string ProviderName = "cardmarket";

    private readonly IProviderSettingsService _settings;

    public CardMarketAuthHandler(IProviderSettingsService settings)
        => _settings = settings;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var appToken     = await _settings.GetAsync(ProviderName, "app_token",     ct) ?? string.Empty;
        var appSecret    = await _settings.GetAsync(ProviderName, "app_secret",    ct) ?? string.Empty;
        var accessToken  = await _settings.GetAsync(ProviderName, "access_token",  ct) ?? string.Empty;
        var accessSecret = await _settings.GetAsync(ProviderName, "access_secret", ct) ?? string.Empty;

        var header = BuildAuthHeader(request, appToken, appSecret, accessToken, accessSecret);
        request.Headers.TryAddWithoutValidation("Authorization", header);

        return await base.SendAsync(request, ct);
    }

    // ── OAuth 1.0a ────────────────────────────────────────────────────────────

    private static string BuildAuthHeader(
        HttpRequestMessage request,
        string consumerKey,
        string consumerSecret,
        string token,
        string tokenSecret)
    {
        var url       = request.RequestUri!.GetLeftPart(UriPartial.Path);
        var method    = request.Method.Method.ToUpperInvariant();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce     = Guid.NewGuid().ToString("N");

        // Paramètres OAuth (sans oauth_signature)
        var oauthParams = new SortedDictionary<string, string>
        {
            ["oauth_consumer_key"]     = consumerKey,
            ["oauth_nonce"]            = nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"]        = timestamp,
            ["oauth_token"]            = token,
            ["oauth_version"]          = "1.0",
        };

        // Paramètres de query string (à inclure dans la base string)
        if (request.RequestUri.Query.Length > 1)
        {
            foreach (var pair in request.RequestUri.Query.TrimStart('?').Split('&'))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                    oauthParams[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }

        var paramString = string.Join("&",
            oauthParams.Select(p => $"{PercentEncode(p.Key)}={PercentEncode(p.Value)}"));

        var baseString = $"{method}&{PercentEncode(url)}&{PercentEncode(paramString)}";
        var signingKey = $"{PercentEncode(consumerSecret)}&{PercentEncode(tokenSecret)}";
        var signature  = ComputeHmacSha1(signingKey, baseString);

        // Build Authorization header (contient uniquement les paramètres oauth_*)
        var headerParams = new Dictionary<string, string>
        {
            ["oauth_consumer_key"]     = consumerKey,
            ["oauth_nonce"]            = nonce,
            ["oauth_signature"]        = signature,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"]        = timestamp,
            ["oauth_token"]            = token,
            ["oauth_version"]          = "1.0",
        };

        var headerValue = "OAuth " + string.Join(", ",
            headerParams.Select(p => $"{p.Key}=\"{PercentEncode(p.Value)}\""));

        return headerValue;
    }

    private static string ComputeHmacSha1(string key, string data)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
#pragma warning disable CA5350 // HMACSHA1 requis par le protocole OAuth 1.0a — pas de choix algorithmique
        using var hmac = new HMACSHA1(keyBytes);
#pragma warning restore CA5350
        return Convert.ToBase64String(hmac.ComputeHash(dataBytes));
    }

    private static string PercentEncode(string value)
        => Uri.EscapeDataString(value)
            .Replace("+", "%20")
            .Replace("*", "%2A")
            .Replace("%7E", "~");
}
