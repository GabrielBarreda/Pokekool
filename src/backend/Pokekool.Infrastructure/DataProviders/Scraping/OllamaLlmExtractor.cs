using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pokekool.Application.DataCollection;
using Pokekool.Infrastructure.Options;

namespace Pokekool.Infrastructure.DataProviders.Scraping;

/// <summary>
/// Extracteur LLM utilisant Ollama (API locale) pour parser du HTML en JSON structuré.
/// </summary>
public sealed class OllamaLlmExtractor : ILlmExtractor, IDisposable
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaLlmExtractor> _logger;

    private static readonly CompositeFormat PromptTemplate = CompositeFormat.Parse(
        """
        Tu es un extracteur de données structurées.
        Voici le contenu d'une page web (nettoyé). Extrais uniquement les informations correspondant au schéma JSON suivant.
        Réponds UNIQUEMENT avec un JSON valide, sans texte, sans markdown, sans balises de code.

        Schéma attendu :
        {0}

        Contenu :
        {1}
        """);

    public OllamaLlmExtractor(IOptions<OllamaOptions> options, ILogger<OllamaLlmExtractor> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
        };
    }

    public void Dispose() => _http.Dispose();

    public async Task<string?> ExtractJsonAsync(string htmlContent, string jsonSchema, CancellationToken ct = default)
    {
        var prompt = string.Format(null, PromptTemplate, jsonSchema, htmlContent);

        var requestBody = new
        {
            model = _options.Model,
            prompt,
            stream = false,
            format = "json",
            options = new { num_predict = _options.MaxTokens }
        };

        try
        {
            var response = await _http.PostAsJsonAsync("/api/generate", requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama a retourné {Status} pour l'extraction", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseEl))
            {
                _logger.LogWarning("Réponse Ollama sans champ 'response'");
                return null;
            }

            var jsonText = responseEl.GetString();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Champ 'response' Ollama vide");
                return null;
            }

            // Validation : s'assurer que c'est du JSON valide
            try
            {
                JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Ollama a retourné un JSON invalide (tronqué ici pour sécurité)");
                return null;
            }

            return jsonText;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout Ollama après {Timeout}s", _options.TimeoutSeconds);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama indisponible ({BaseUrl}) — dégradation gracieuse", _options.BaseUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de l'appel Ollama");
            return null;
        }
    }
}
