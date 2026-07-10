using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pokekool.Application.Chat;
using Pokekool.Infrastructure.Options;

namespace Pokekool.Infrastructure.Chat;

/// <summary>
/// Client Ollama dédié au chatbot — utilise l'endpoint /api/chat (function calling),
/// distinct de OllamaLlmExtractor qui reste sur /api/generate pour le scraping.
/// </summary>
public sealed class OllamaChatClient : IOllamaChatClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly OllamaChatOptions _options;
    private readonly ILogger<OllamaChatClient> _logger;

    public OllamaChatClient(IOptions<OllamaChatOptions> options, ILogger<OllamaChatClient> logger)
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

    public async Task<OllamaChatResponse> SendAsync(
        IReadOnlyList<OllamaChatMessage> messages,
        IReadOnlyList<OllamaToolDefinition> tools,
        CancellationToken ct = default)
    {
        var body = new
        {
            model = _options.Model,
            stream = false,
            messages = messages.Select(m => new { role = m.Role, content = m.Content ?? string.Empty }),
            tools = tools.Select(t => new
            {
                type = "function",
                function = new { name = t.Name, description = t.Description, parameters = t.ParametersSchema }
            }),
            options = new { num_predict = _options.MaxTokens }
        };

        try
        {
            var response = await _http.PostAsJsonAsync("/api/chat", body, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama /api/chat a retourné {Status}", response.StatusCode);
                return new OllamaChatResponse("Désolé, je suis momentanément indisponible.", []);
            }

            var raw = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(raw);
            var msg = doc.RootElement.GetProperty("message");

            var toolCalls = new List<OllamaToolCall>();
            if (msg.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in tc.EnumerateArray())
                {
                    var fn = call.GetProperty("function");
                    var name = fn.GetProperty("name").GetString()!;
                    var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    if (fn.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in argsEl.EnumerateObject())
                            args[prop.Name] = ToClrValue(prop.Value);
                    }
                    toolCalls.Add(new OllamaToolCall(name, args));
                }
            }

            var content = msg.TryGetProperty("content", out var c) ? c.GetString() : null;
            return new OllamaChatResponse(content, toolCalls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur appel Ollama /api/chat");
            return new OllamaChatResponse("Erreur lors de la communication avec le modèle.", []);
        }
    }

    private static object? ToClrValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => el.GetRawText()
    };
}
