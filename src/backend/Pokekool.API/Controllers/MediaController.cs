using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pokekool.API.Controllers;

/// <summary>
/// Proxy média same-origin. Les images externes (ex. images.pokemontcg.io) n'envoient pas
/// d'en-tête CORS et ne peuvent donc pas être utilisées comme textures WebGL (présentoir 3D).
/// Ce proxy les re-sert depuis la même origine que le front. Anonyme (les requêtes d'image
/// n'envoient pas de token), avec garde anti-SSRF basique.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media")]
[AllowAnonymous]
public sealed class MediaController : ControllerBase
{
    private const long MaxBytes = 15_000_000; // 15 Mo
    private readonly IHttpClientFactory _httpFactory;

    public MediaController(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    [HttpGet("image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetImage([FromQuery] string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            IsBlockedHost(uri))
        {
            return BadRequest(new { message = "URL invalide." });
        }

        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (compatible; PokekoolMediaProxy/1.0)");

        using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, new { message = "Ressource distante indisponible." });

        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "La ressource n'est pas une image." });

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length > MaxBytes)
            return BadRequest(new { message = "Image trop volumineuse." });

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(bytes, contentType);
    }

    /// <summary>Bloque les hôtes locaux / privés (anti-SSRF sommaire).</summary>
    private static bool IsBlockedHost(Uri uri)
    {
        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (IPAddress.TryParse(host, out var ip))
        {
            var b = ip.GetAddressBytes();
            if (IPAddress.IsLoopback(ip)) return true;
            if (b.Length == 4)
            {
                if (b[0] == 10) return true;                          // 10.0.0.0/8
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true; // 172.16.0.0/12
                if (b[0] == 192 && b[1] == 168) return true;         // 192.168.0.0/16
                if (b[0] == 169 && b[1] == 254) return true;         // link-local
            }
        }
        return false;
    }
}
