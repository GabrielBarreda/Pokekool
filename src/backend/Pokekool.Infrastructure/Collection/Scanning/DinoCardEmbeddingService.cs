using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Collection.Scanning;

namespace Pokekool.Infrastructure.Collection.Scanning;

/// <summary>
/// Client du service d'embedding visuel (DINOv2, service Python séparé conteneurisé —
/// voir services/card-embedding/). Même contrat de dégradation gracieuse que
/// <see cref="OllamaCardVisionAnalyzer"/> : tout échec (timeout, HTTP, JSON invalide) est
/// converti en <see cref="CardEmbeddingResult"/> avec un statut explicite, jamais une exception.
/// HttpClient injecté via AddHttpClient&lt;ICardEmbeddingService, DinoCardEmbeddingService&gt;
/// (BaseAddress/Timeout configurés côté DependencyInjection, pas ici).
/// </summary>
public sealed class DinoCardEmbeddingService : ICardEmbeddingService
{
    private readonly HttpClient _http;
    private readonly ILogger<DinoCardEmbeddingService> _logger;

    public DinoCardEmbeddingService(HttpClient http, ILogger<DinoCardEmbeddingService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<CardEmbeddingResult> EmbedAsync(string imageBase64, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/embed", new { imageBase64 }, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response, ct);
                _logger.LogWarning("Card Embedding a retourné {Status} : {Body}", response.StatusCode, body);
                return CardEmbeddingResult.Failure(
                    CardEmbeddingStatus.ServiceUnavailable,
                    $"Service d'embedding a répondu {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<EmbedResponseDto>(cancellationToken: ct);
            if (payload?.Embedding is null || payload.Embedding.Length == 0)
            {
                _logger.LogWarning("Card Embedding a renvoyé une réponse vide/illisible");
                return CardEmbeddingResult.Failure(CardEmbeddingStatus.InvalidResponse, "Réponse d'embedding vide.");
            }

            return CardEmbeddingResult.Success(payload.Embedding);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout Card Embedding");
            return CardEmbeddingResult.Failure(CardEmbeddingStatus.ServiceUnavailable, "Timeout du service d'embedding.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Card Embedding indisponible ({BaseAddress})", _http.BaseAddress);
            return CardEmbeddingResult.Failure(
                CardEmbeddingStatus.ServiceUnavailable,
                $"Service injoignable ({_http.BaseAddress}).");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Card Embedding a retourné un JSON invalide");
            return CardEmbeddingResult.Failure(CardEmbeddingStatus.InvalidResponse, "JSON invalide renvoyé par le service.");
        }
    }

    public async Task<CardEmbeddingResult> EmbedFromUrlAsync(string imageUrl, CancellationToken ct = default)
    {
        // HttpClient séparé (pas _http, dont la BaseAddress pointe vers le service d'embedding) —
        // celui-ci télécharge une image arbitraire (ex: image officielle PokemonTCG.io), utilisée
        // uniquement par le job d'indexation du catalogue.
        try
        {
            using var imageHttp = new HttpClient();
            var imageBytes = await imageHttp.GetByteArrayAsync(imageUrl, ct);
            return await EmbedAsync(Convert.ToBase64String(imageBytes), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Téléchargement image échoué ({Url})", imageUrl);
            return CardEmbeddingResult.Failure(
                CardEmbeddingStatus.ServiceUnavailable, $"Téléchargement image échoué ({imageUrl}).");
        }
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return string.Empty; }
    }

    private sealed record EmbedResponseDto(float[]? Embedding, int Dim);
}
