using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Infrastructure.DataProviders.Scraping;

namespace Pokekool.API.Controllers;

/// <summary>
/// Endpoint de test du pipeline scraping — À SUPPRIMER après validation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/scraping")]
[Produces("application/json")]
[AllowAnonymous] // Temporaire : à sécuriser ou supprimer en prod
public sealed class AdminScrapingController : ControllerBase
{
    private readonly ScrapingPipeline _pipeline;

    // Schéma de test générique — à adapter selon le site cible
    private const string TestSchema =
        """
        {
          "name": "string (nom du produit)",
          "price": "number (prix en USD, null si absent)",
          "grade": "string (grade PSA si présent, sinon null)",
          "condition": "string (condition de la carte si présente)"
        }
        """;

    public AdminScrapingController(ScrapingPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    /// <summary>
    /// Teste le pipeline complet : Playwright → HtmlCleaner → Ollama → JSON.
    /// </summary>
    /// <param name="url">URL à scraper (ex: https://www.pricecharting.com/game/pokemon-base-set/charizard)</param>
    /// <param name="ct">Token d'annulation</param>
    [HttpPost("test")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestScraping([FromQuery] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            return BadRequest(new { error = "URL invalide ou manquante" });

        var result = await _pipeline.ExecuteAsync<Dictionary<string, object?>>(url, TestSchema, ct: ct);

        if (result is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Pipeline échoué — vérifier Playwright et Ollama" });

        return Ok(result);
    }
}
