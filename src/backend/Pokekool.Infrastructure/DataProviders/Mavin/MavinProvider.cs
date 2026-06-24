using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.DataProviders.Scraping;

namespace Pokekool.Infrastructure.DataProviders.Mavin;

/// <summary>
/// Provider Mavin.io — prix moyens issus des ventes eBay complétées.
/// Mavin est un agrégateur public : pas besoin de compte eBay Developer.
///
/// Pipeline : Playwright → HtmlCleaner → OllamaLlmExtractor → MavinResult → CardPriceData
///
/// Source de données : https://mavin.io (scraping public, aucune clé requise)
/// </summary>
public sealed class MavinProvider : ICardPriceProvider
{
    public string Name => "mavin";
    public ProviderCapability Capabilities =>
        ProviderCapability.MarketPrices | ProviderCapability.SalesHistory;

    private static readonly string JsonSchema =
        """
        {
          "averagePrice": 0.0,
          "lowPrice": 0.0,
          "highPrice": 0.0,
          "salesCount": 0
        }
        """;

    private static readonly ScraperOptions PlaywrightOptions = new(
        WaitForNetworkIdleMs: 3000,
        WaitForSelector: null,
        BlockImages: true,
        BlockFonts: true,
        Headers: new Dictionary<string, string>
        {
            ["Accept-Language"] = "en-US,en;q=0.9",
        });

    private readonly ScrapingPipeline       _pipeline;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IPokemonSetRepository  _setRepo;
    private readonly ILogger<MavinProvider> _logger;
    private readonly Random _rng = new();

    public MavinProvider(
        ScrapingPipeline       pipeline,
        IPokemonCardRepository cardRepo,
        IPokemonSetRepository  setRepo,
        ILogger<MavinProvider> logger)
    {
        _pipeline = pipeline;
        _cardRepo = cardRepo;
        _setRepo  = setRepo;
        _logger   = logger;
    }

    public async Task<CardPriceData?> GetPriceAsync(
        string cardIdentifier, CancellationToken ct = default)
    {
        var (cardName, setName) = await ResolveNamesAsync(cardIdentifier, ct);
        if (cardName is null || setName is null)
        {
            _logger.LogWarning("Mavin — noms introuvables pour '{Id}'", cardIdentifier);
            return null;
        }

        var url = MavinUrlBuilder.BuildUrl(cardName, setName);
        if (url is null) return null;

        _logger.LogInformation("Mavin — fetch {Url}", url);

        try
        {
            var raw = await _pipeline.ExecuteAsync<MavinResult>(
                url, JsonSchema, PlaywrightOptions, ct);

            if (raw?.AveragePrice is null or 0) return null;

            return new CardPriceData(
                Source:         "mavin",
                RawMarketPrice: raw.AveragePrice,
                Psa7:           null,
                Psa8:           null,
                Psa9:           null,
                Psa10:          null,
                Currency:       "USD",
                FetchedAt:      DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mavin — erreur scraping pour '{Id}'", cardIdentifier);
            return null;
        }
    }

    public async Task<IReadOnlyList<CardPriceData>> GetPricesBatchAsync(
        IEnumerable<string> cardIdentifiers, CancellationToken ct = default)
    {
        var results = new List<CardPriceData>();

        foreach (var id in cardIdentifiers)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var price = await GetPriceAsync(id, ct);
                if (price is not null) results.Add(price);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mavin — erreur ignorée pour '{Id}' dans le batch", id);
            }

            // Rate limiting : 2–4 s entre requêtes
            await Task.Delay(_rng.Next(2000, 4001), ct);
        }

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(string? CardName, string? SetName)> ResolveNamesAsync(
        string tcgId, CancellationToken ct)
    {
        try
        {
            var card = await _cardRepo.GetByTcgIdAsync(tcgId, ct);
            if (card is null) return (null, null);

            var set = await _setRepo.GetByIdAsync(card.SetId, ct);
            return (card.Name, set?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Mavin — DB lookup échoué pour '{Id}'", tcgId);
            return (null, null);
        }
    }

    private sealed record MavinResult(
        decimal? AveragePrice,
        decimal? LowPrice,
        decimal? HighPrice,
        int?     SalesCount);
}
