using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.DataProviders.Scraping;

namespace Pokekool.Infrastructure.DataProviders.PointSales;

/// <summary>
/// Provider 130point.com — ventes eBay complétées (historique de ventes réalisées).
/// Agrégateur public : aucun compte eBay Developer requis.
///
/// Pipeline : Playwright → HtmlCleaner → OllamaLlmExtractor → PointSalesResult → CardPriceData
///
/// Source de données : https://130point.com (scraping public, aucune clé requise)
/// Complément à Mavin : 130point expose l'historique brut des ventes individuelles.
/// </summary>
public sealed class PointSalesProvider : ICardPriceProvider
{
    public string Name => "130point";
    public ProviderCapability Capabilities =>
        ProviderCapability.MarketPrices | ProviderCapability.SalesHistory;

    private static readonly string JsonSchema =
        """
        {
          "averagePrice": 0.0,
          "lastSalePrice": 0.0,
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

    private readonly ScrapingPipeline          _pipeline;
    private readonly IPokemonCardRepository    _cardRepo;
    private readonly IPokemonSetRepository     _setRepo;
    private readonly ILogger<PointSalesProvider> _logger;
    private readonly Random _rng = new();

    public PointSalesProvider(
        ScrapingPipeline          pipeline,
        IPokemonCardRepository    cardRepo,
        IPokemonSetRepository     setRepo,
        ILogger<PointSalesProvider> logger)
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
            _logger.LogWarning("130point — noms introuvables pour '{Id}'", cardIdentifier);
            return null;
        }

        var url = PointSalesUrlBuilder.BuildUrl(cardName, setName);
        if (url is null) return null;

        _logger.LogInformation("130point — fetch {Url}", url);

        try
        {
            var raw = await _pipeline.ExecuteAsync<PointSalesResult>(
                url, JsonSchema, PlaywrightOptions, ct);

            // On préfère averagePrice, sinon lastSalePrice
            var price = raw?.AveragePrice ?? raw?.LastSalePrice;
            if (price is null or 0) return null;

            return new CardPriceData(
                Source:         "130point",
                RawMarketPrice: price,
                Psa7:           null,
                Psa8:           null,
                Psa9:           null,
                Psa10:          null,
                Currency:       "USD",
                FetchedAt:      DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "130point — erreur scraping pour '{Id}'", cardIdentifier);
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
                _logger.LogWarning(ex, "130point — erreur ignorée pour '{Id}' dans le batch", id);
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
            _logger.LogDebug(ex, "130point — DB lookup échoué pour '{Id}'", tcgId);
            return (null, null);
        }
    }

    private sealed record PointSalesResult(
        decimal? AveragePrice,
        decimal? LastSalePrice,
        int?     SalesCount);
}
