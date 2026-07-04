using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Infrastructure.DataProviders.Scraping;

namespace Pokekool.Infrastructure.DataProviders.Psa;

/// <summary>
/// Provider PSA — scrape les données de population (grades, gem rate).
/// URL pattern : https://www.psacard.com/pop/trading-card-games/pokemon/{set-slug}/
/// </summary>
public sealed class PsaProvider : IPopulationProvider
{
    public string Name => "psa";
    public ProviderCapability Capabilities => ProviderCapability.Population;

    private static readonly string JsonSchema =
        """
        {
          "cardName": "",
          "set": "",
          "number": "",
          "totalGraded": 0,
          "pop10": 0,
          "pop9": 0,
          "pop8": 0,
          "pop7": 0,
          "gemRate": 0.0
        }
        """;

    private const string BaseUrl = "https://www.psacard.com/pop/trading-card-games/pokemon";

    private readonly ScrapingPipeline _pipeline;
    private readonly ILogger<PsaProvider> _logger;

    public PsaProvider(ScrapingPipeline pipeline, ILogger<PsaProvider> logger)
    {
        _pipeline = pipeline;
        _logger   = logger;
    }

    public async Task<IReadOnlyList<CardPopulationData>?> GetPopulationAsync(
        CardPopulationRequest request, CancellationToken ct = default)
    {
        var setSlug = NormalizeSlug(request.SetName);
        var url     = $"{BaseUrl}/{setSlug}/";

        _logger.LogInformation("PSA — fetch {Url} pour '{CardName}'", url, request.CardName);

        try
        {
            var raw = await _pipeline.ExecuteAsync<PsaResult>(url, JsonSchema, ct: ct);
            // Si aucune donnée utile (bot-block, page vide) → ne pas créer de ligne vide
            if (raw is null || (raw.TotalGraded is null && raw.Pop10 is null)) return null;
            return [MapToPopulationData(raw)];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "PSA — erreur scraping pour '{CardName}' / '{SetName}'",
                request.CardName, request.SetName);
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string NormalizeSlug(string name)
        => name.Trim().ToLowerInvariant()
               .Replace(' ', '-')
               .Replace("&", "and")
               .Replace("'", string.Empty);

    private static CardPopulationData MapToPopulationData(PsaResult r)
    {
        // Calculer gem rate si non fourni par le LLM
        double? gemRate = r.GemRate > 0
            ? r.GemRate
            : (r.TotalGraded > 0 && r.Pop10 is not null
                ? Math.Round((double)r.Pop10.Value / r.TotalGraded!.Value * 100, 2)
                : null);

        return new CardPopulationData(
            Source:    "psa",
            Total:     r.TotalGraded,
            Pop10:     r.Pop10,
            Pop9:      r.Pop9,
            Pop8:      r.Pop8,
            Pop7:      r.Pop7,
            GemRate10: gemRate,
            FetchedAt: DateTime.UtcNow);
    }

    // ── DTO interne ───────────────────────────────────────────────────────────

    private sealed record PsaResult(
        string?  CardName,
        string?  Set,
        string?  Number,
        int?     TotalGraded,
        int?     Pop10,
        int?     Pop9,
        int?     Pop8,
        int?     Pop7,
        double?  GemRate);
}
