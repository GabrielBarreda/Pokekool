using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;

namespace Pokekool.Infrastructure.DataProviders.Scraping;

/// <summary>
/// Coordinateur du pipeline de scraping :
/// Playwright (HTML brut) → HtmlCleaner → OllamaLlmExtractor (JSON) → Désérialisation → T
/// </summary>
public sealed class ScrapingPipeline
{
    private readonly IWebScraper _scraper;
    private readonly ILlmExtractor _llm;
    private readonly ILogger<ScrapingPipeline> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ScrapingPipeline(IWebScraper scraper, ILlmExtractor llm, ILogger<ScrapingPipeline> logger)
    {
        _scraper = scraper;
        _llm = llm;
        _logger = logger;
    }

    /// <summary>
    /// Exécute le pipeline complet et retourne un objet typé ou null si une étape échoue.
    /// </summary>
    /// <typeparam name="T">Type cible de la désérialisation.</typeparam>
    /// <param name="url">URL à scraper.</param>
    /// <param name="jsonSchema">Schéma JSON à fournir au LLM.</param>
    /// <param name="scraperOptions">Options Playwright optionnelles.</param>
    /// <param name="ct">Token d'annulation.</param>
    public async Task<T?> ExecuteAsync<T>(
        string url,
        string jsonSchema,
        ScraperOptions? scraperOptions = null,
        CancellationToken ct = default) where T : class
    {
        // Étape 1 : Fetch HTML
        _logger.LogInformation("ScrapingPipeline — fetch {Url}", url);
        var rawHtml = await _scraper.FetchPageHtmlAsync(url, scraperOptions, ct);
        if (rawHtml is null)
        {
            _logger.LogWarning("ScrapingPipeline — échec fetch HTML pour {Url}", url);
            return null;
        }

        // Étape 2 : Nettoyage HTML
        var cleanedContent = HtmlCleaner.Clean(rawHtml);
        _logger.LogDebug("ScrapingPipeline — HTML nettoyé : {CharCount} caractères", cleanedContent.Length);

        if (string.IsNullOrWhiteSpace(cleanedContent))
        {
            _logger.LogWarning("ScrapingPipeline — contenu HTML vide après nettoyage pour {Url}", url);
            return null;
        }

        // Étape 3 : Extraction LLM
        var jsonString = await _llm.ExtractJsonAsync(cleanedContent, jsonSchema, ct);
        if (jsonString is null)
        {
            _logger.LogWarning("ScrapingPipeline — LLM n'a pas retourné de JSON pour {Url}", url);
            return null;
        }

        // Étape 4 : Désérialisation
        try
        {
            var result = JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            if (result is null)
                _logger.LogWarning("ScrapingPipeline — désérialisation nulle pour {Url}", url);
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "ScrapingPipeline — échec désérialisation JSON pour {Url}", url);
            return null;
        }
    }
}
