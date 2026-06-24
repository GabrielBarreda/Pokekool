using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.DataProviders.Scraping;

namespace Pokekool.Infrastructure.DataProviders.CardTrader;

/// <summary>
/// Provider CardTrader — scrape les prix de marché EUR (stock live, site public).
///
/// Pipeline : Playwright → HtmlCleaner → OllamaLlmExtractor → CardTraderResult → CardPriceData
///
/// Source de données : https://www.cardtrader.com (scraping public, aucune clé requise)
/// </summary>
public sealed class CardTraderProvider : ICardPriceProvider
{
    public string Name => "cardtrader";
    public ProviderCapability Capabilities => ProviderCapability.MarketPrices;

    private static readonly string JsonSchema =
        """
        {
          "lowestPrice": 0.0,
          "currency": "EUR",
          "stock": 0
        }
        """;

    private static readonly ScraperOptions PlaywrightOptions = new(
        WaitForNetworkIdleMs: 3000,
        WaitForSelector: null,
        BlockImages: true,
        BlockFonts: true,
        Headers: new Dictionary<string, string>
        {
            ["Accept-Language"] = "fr-FR,fr;q=0.9,en;q=0.8",
        });

    private readonly ScrapingPipeline       _pipeline;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IPokemonSetRepository  _setRepo;
    private readonly ILogger<CardTraderProvider> _logger;
    private readonly Random _rng = new();

    public CardTraderProvider(
        ScrapingPipeline       pipeline,
        IPokemonCardRepository cardRepo,
        IPokemonSetRepository  setRepo,
        ILogger<CardTraderProvider> logger)
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
            _logger.LogWarning("CardTrader — noms introuvables pour '{Id}'", cardIdentifier);
            return null;
        }

        var url = CardTraderUrlBuilder.BuildUrl(cardName, setName);
        if (url is null) return null;

        _logger.LogInformation("CardTrader — fetch {Url}", url);

        try
        {
            var raw = await _pipeline.ExecuteAsync<CardTraderResult>(
                url, JsonSchema, PlaywrightOptions, ct);

            if (raw?.LowestPrice is null or 0) return null;

            return new CardPriceData(
                Source:         "cardtrader",
                RawMarketPrice: raw.LowestPrice,
                Psa7:           null,
                Psa8:           null,
                Psa9:           null,
                Psa10:          null,
                Currency:       raw.Currency ?? "EUR",
                FetchedAt:      DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CardTrader — erreur scraping pour '{Id}'", cardIdentifier);
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
                _logger.LogWarning(ex, "CardTrader — erreur ignorée pour '{Id}' dans le batch", id);
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
            _logger.LogDebug(ex, "CardTrader — DB lookup échoué pour '{Id}'", tcgId);
            return (null, null);
        }
    }

    private sealed record CardTraderResult(
        decimal? LowestPrice,
        string?  Currency,
        int?     Stock);
}
