using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pokekool.Application.Abstractions;
using Pokekool.Infrastructure.Options;

namespace Pokekool.Infrastructure.ExternalClients;

/// <summary>
/// Client HTTP pour l'API TCGdex (https://api.tcgdex.net) — récupère les noms de cartes
/// localisés, un set entier par appel (GET /v2/{lang}/sets/{setId}) plutôt que carte par carte.
/// </summary>
internal sealed class TcgDexClient : ITcgDexClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<TcgDexClient> _logger;

    public TcgDexClient(HttpClient http, IOptions<TcgDexOptions> options, ILogger<TcgDexClient> logger)
    {
        _http = http;
        _logger = logger;
        _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + '/');
    }

    public async Task<IReadOnlyList<TcgDexCardNameDto>> GetCardNamesBySetAsync(
        string setId, string language, CancellationToken ct = default)
    {
        var lang = language.Trim().ToLowerInvariant();
        var response = await _http.GetAsync($"{lang}/sets/{setId}", ct);

        if (!response.IsSuccessStatusCode)
        {
            // Set absent côté TCGdex (ex. très ancien) ou langue non couverte : repli EN,
            // pas une erreur fatale pour le sync — voir SyncCardTranslationsCommandHandler.
            _logger.LogWarning(
                "[TCGdex] set '{SetId}' (langue {Language}) : HTTP {Status}, ignoré (repli EN).",
                setId, lang, (int)response.StatusCode);
            return [];
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var set = JsonSerializer.Deserialize<TcgDexSetResponse>(content, JsonOptions);

        if (set?.Cards is null)
            return [];

        return set.Cards
            .Where(c => !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new TcgDexCardNameDto(c.Id, c.Name))
            .ToList();
    }

    // ---- Internal deserialization models ----

    private sealed class TcgDexSetResponse
    {
        [JsonPropertyName("cards")]
        public List<TcgDexCardResponse>? Cards { get; set; }
    }

    private sealed class TcgDexCardResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;
    }
}
