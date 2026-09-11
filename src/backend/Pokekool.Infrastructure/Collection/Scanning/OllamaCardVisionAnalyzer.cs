using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Infrastructure.Options;

namespace Pokekool.Infrastructure.Collection.Scanning;

/// <summary>
/// Analyseur de cartes Pokémon via Ollama Vision (modèle multimodal type llava).
/// Dégradation gracieuse : tout échec (timeout, HTTP, modèle absent, JSON invalide) est
/// converti en <see cref="CardVisionAnalysis"/> avec un <see cref="CardVisionStatus"/> explicite
/// (jamais d'exception), pour que le client sache POURQUOI le résultat est vide.
/// </summary>
public sealed class OllamaCardVisionAnalyzer : ICardVisionAnalyzer, IDisposable
{
    private const string PromptTemplate =
        """
        Tu es un expert en cartes Pokémon TCG avec une connaissance exhaustive de toutes les extensions.
        Tu analyses une photo contenant une ou plusieurs cartes Pokémon (posées à plat, face visible).

        CONTEXTE CATALOGUE (cartes déjà connues de notre système) :
        {ragContext}

        Pour CHAQUE carte visible dans l'image, retourne un objet JSON.
        Réponds UNIQUEMENT avec un tableau JSON valide — aucun texte, aucun markdown, aucune balise.

        Format attendu :
        [
          {
            "name": "Nom exact de la carte tel qu'imprimé",
            "setName": "Nom complet de l'extension",
            "number": "NNN/TTT tel qu'imprimé (ex: 025/165)",
            "gradingCompany": "PSA" | "BGS" | "CGC" | null,
            "gradeValue": 9.5 | null,
            "certNumber": "12345678" | null,
            "confidence": 0.85
          }
        ]

        RÈGLES :
        - Si la carte est dans un slab (boîtier plastique rigide avec étiquette colorée) : lis gradingCompany, gradeValue et certNumber sur l'étiquette
        - PSA = étiquette rouge/bleue ; BGS = étiquette noire/or ; CGC = étiquette verte
        - Le numéro de certification est le grand nombre imprimé sur l'étiquette
        - Si une information n'est pas lisible, utilise null
        - La confidence doit refléter ta certitude réelle (0.0 = inconnu, 1.0 = certain)
        - Si tu identifies une carte grâce au contexte catalogue, booste ta confidence
        """;

    private readonly HttpClient _http;
    private readonly OllamaVisionOptions _options;
    private readonly ILogger<OllamaCardVisionAnalyzer> _logger;

    public OllamaCardVisionAnalyzer(
        IOptions<OllamaVisionOptions> options,
        ILogger<OllamaCardVisionAnalyzer> logger)
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

    public async Task<CardVisionAnalysis> AnalyzeAsync(
        string imageBase64,
        string ragContext,
        IReadOnlyList<string>? exclusions = null,
        string? modelOverride = null,
        CancellationToken ct = default)
    {
        // Modèle réglable dynamiquement (Paramètres › Scan, IA Vision — voir
        // VisionModelSettingsReader) sans redémarrer l'API : _options.Model (config statique,
        // résolu au démarrage) reste le filet de sécurité si l'appelant ne passe rien.
        var model = string.IsNullOrWhiteSpace(modelOverride) ? _options.Model : modelOverride;

        var prompt = PromptTemplate.Replace("{ragContext}", ragContext, StringComparison.Ordinal);

        // Re-roll : l'utilisateur a rejeté ces identifications, on interdit au modèle de les reproposer
        // et on l'oriente vers les indices fiables (numéro imprimé, symbole d'extension).
        if (exclusions is { Count: > 0 })
        {
            var list = string.Join("\n", exclusions.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => $"- {e.Trim()}"));
            prompt +=
                "\n\nIDENTIFICATIONS DÉJÀ REJETÉES PAR L'UTILISATEUR (INTERDIT de les reproposer) :\n" + list +
                "\n\nCes propositions étaient FAUSSES. Ne les répète pas. Réexamine attentivement la carte : " +
                "lis en priorité le NUMÉRO imprimé (ex: 114/127) en bas de la carte et le symbole d'extension, " +
                "et propose une identification DIFFÉRENTE et cohérente avec ces indices.";
        }

        var requestBody = new
        {
            model,
            prompt,
            images = new[] { imageBase64 },
            stream = false,
            format = "json",
            // temperature: 0 + seed fixe : sans ça le modèle peut lire des choses différentes
            // sur la MÊME photo d'un appel à l'autre (observé en test le 2026-09-11 — une carte
            // correctement identifiée à un instant t, puis une identification totalement
            // différente et fausse à la relance suivante sur la photo identique). Le fallback
            // texte/vision dépend justement de relances répétées sur une même photo (3e tentative
            // puis au-delà) : la reproductibilité y compte plus que la diversité de réponses.
            options = new { num_predict = 2048, temperature = 0, seed = 42 }
        };

        try
        {
            var response = await _http.PostAsJsonAsync("/api/generate", requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response, ct);

                // Ollama renvoie 404 + "model '...' not found, try pulling it first" quand le modèle
                // n'est pas téléchargé — cas très fréquent, à distinguer d'une vraie panne service.
                if (response.StatusCode == HttpStatusCode.NotFound
                    || body.Contains("not found", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("try pulling", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Modèle Ollama Vision '{Model}' absent : {Body}", model, body);
                    return CardVisionAnalysis.Failure(
                        CardVisionStatus.ModelNotFound,
                        $"Modèle '{model}' absent (ollama pull {model}).");
                }

                _logger.LogWarning("Ollama Vision a retourné {Status} : {Body}", response.StatusCode, body);
                return CardVisionAnalysis.Failure(
                    CardVisionStatus.ServiceUnavailable,
                    $"Ollama a répondu {(int)response.StatusCode}.");
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            using var envelope = JsonDocument.Parse(content);

            if (!envelope.RootElement.TryGetProperty("response", out var responseEl))
            {
                _logger.LogWarning("Réponse Ollama Vision sans champ 'response'");
                return CardVisionAnalysis.Failure(CardVisionStatus.InvalidResponse, "Réponse sans champ 'response'.");
            }

            var jsonText = responseEl.GetString();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Champ 'response' Ollama Vision vide");
                return CardVisionAnalysis.Failure(CardVisionStatus.InvalidResponse, "Le modèle a renvoyé une réponse vide.");
            }

            return CardVisionAnalysis.Success(ParseCards(jsonText));
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout Ollama Vision après {Timeout}s", _options.TimeoutSeconds);
            return CardVisionAnalysis.Failure(
                CardVisionStatus.ServiceUnavailable,
                $"Timeout après {_options.TimeoutSeconds}s.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama Vision indisponible ({BaseUrl})", _options.BaseUrl);
            return CardVisionAnalysis.Failure(
                CardVisionStatus.ServiceUnavailable,
                $"Service injoignable ({_options.BaseUrl}).");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ollama Vision a retourné un JSON invalide");
            return CardVisionAnalysis.Failure(CardVisionStatus.InvalidResponse, "JSON invalide renvoyé par le modèle.");
        }
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return string.Empty; }
    }

    private IReadOnlyList<CardVisionResult> ParseCards(string jsonText)
    {
        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        var array = root.ValueKind switch
        {
            JsonValueKind.Array => root,
            // Certains modèles enveloppent le tableau : { "cards": [...] }
            JsonValueKind.Object => FindFirstArray(root),
            _ => (JsonElement?)null
        };

        var results = new List<CardVisionResult>();

        if (array is { ValueKind: JsonValueKind.Array } arr)
        {
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.Object)
                    results.Add(MapCard(el));
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            // Objet unique décrivant une seule carte.
            results.Add(MapCard(root));
        }

        return results;
    }

    private static JsonElement? FindFirstArray(JsonElement obj)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
                return prop.Value;
        }
        return null;
    }

    private static CardVisionResult MapCard(JsonElement el) => new(
        DetectedName: GetString(el, "name"),
        DetectedSetName: GetString(el, "setName"),
        DetectedNumber: GetString(el, "number"),
        GradingCompany: GetString(el, "gradingCompany"),
        GradeValue: GetDecimal(el, "gradeValue"),
        CertNumber: GetString(el, "certNumber"),
        Confidence: GetDouble(el, "confidence"));

    private static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p) || p.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var value = p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal? GetDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;

        return p.ValueKind switch
        {
            JsonValueKind.Number when p.TryGetDecimal(out var d) => d,
            JsonValueKind.String when decimal.TryParse(
                p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            _ => null
        };
    }

    private static double GetDouble(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return 0d;

        return p.ValueKind switch
        {
            JsonValueKind.Number when p.TryGetDouble(out var d) => d,
            JsonValueKind.String when double.TryParse(
                p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            _ => 0d
        };
    }
}
