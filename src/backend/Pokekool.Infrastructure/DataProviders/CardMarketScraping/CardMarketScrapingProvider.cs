using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.DataProviders.Scraping;

namespace Pokekool.Infrastructure.DataProviders.CardMarketScraping;

/// <summary>
/// Provider de prix CardMarket via scraping public (site FR).
/// Remplace l'ancienne intégration API OAuth devenue inaccessible.
///
/// Pipeline : Playwright → HtmlCleaner → OllamaLlmExtractor → CmScrapingResult → CardPriceData
/// </summary>
public sealed class CardMarketScrapingProvider : ICardPriceProvider
{
    public string Name => "cardmarket-scraping";

    public ProviderCapability Capabilities =>
        ProviderCapability.MarketPrices;

    // ── JSON schema envoyé à Ollama ───────────────────────────────────────────
    private static readonly string JsonSchema =
        """
        {
          "trendPrice": 0.0,
          "lowPrice": 0.0,
          "avg30": 0.0
        }
        """;

    // ── Playwright : attendre que la page soit chargée ───────────────────────
    // WaitForSelector supprimé : CardMarket a changé sa structure HTML (.info-list-container
    // n'existe plus). On s'appuie sur networkIdle + délai suffisant.
    private static readonly ScraperOptions PlaywrightOptions = new(
        WaitForNetworkIdleMs: 5000,
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
    private readonly ILogger<CardMarketScrapingProvider> _logger;
    private readonly Random _rng = new();

    public CardMarketScrapingProvider(
        ScrapingPipeline       pipeline,
        IPokemonCardRepository cardRepo,
        IPokemonSetRepository  setRepo,
        ILogger<CardMarketScrapingProvider> logger)
    {
        _pipeline = pipeline;
        _cardRepo = cardRepo;
        _setRepo  = setRepo;
        _logger   = logger;
    }

    // ── ICardPriceProvider ────────────────────────────────────────────────────

    public async Task<CardPriceData?> GetPriceAsync(
        string cardIdentifier, CancellationToken ct = default)
    {
        var url = await BuildUrlAsync(cardIdentifier, ct);
        if (url is null)
        {
            _logger.LogWarning(
                "CardMarketScraping — impossible de construire l'URL pour '{Id}'", cardIdentifier);
            return null;
        }

        _logger.LogInformation("CardMarketScraping — fetch {Url}", url);

        try
        {
            var raw = await _pipeline.ExecuteAsync<CmScrapingResult>(
                url, JsonSchema, PlaywrightOptions, ct);

            if (raw is null)
            {
                _logger.LogWarning(
                    "CardMarketScraping — pipeline n'a retourné aucune donnée pour '{Id}'",
                    cardIdentifier);
                return null;
            }

            _logger.LogDebug(
                "CardMarketScraping — '{Id}' : trend={Trend} low={Low} avg30={Avg30}",
                cardIdentifier, raw.TrendPrice, raw.LowPrice, raw.Avg30);

            return MapToCardPriceData(raw);
        }
        catch (Exception ex)
        {
            // Graceful degradation : logguer et continuer le batch
            _logger.LogWarning(
                ex, "CardMarketScraping — erreur scraping pour '{Id}'", cardIdentifier);
            return null;
        }
    }

    public async Task<IReadOnlyList<CardPriceData>> GetPricesBatchAsync(
        IEnumerable<string> cardIdentifiers, CancellationToken ct = default)
    {
        var results = new List<CardPriceData>();

        foreach (var id in cardIdentifiers)
        {
            if (ct.IsCancellationRequested) break;

            // Graceful degradation : une erreur sur une carte n'arrête pas le batch
            CardPriceData? price = null;
            try
            {
                price = await GetPriceAsync(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "CardMarketScraping — erreur ignorée pour '{Id}' dans le batch", id);
            }

            if (price is not null)
                results.Add(price);

            // Rate limiting : délai aléatoire 2–4 s entre chaque requête
            var delayMs = _rng.Next(2000, 4001);
            _logger.LogDebug(
                "CardMarketScraping — pause {DelayMs} ms avant la prochaine requête", delayMs);
            await Task.Delay(delayMs, ct);
        }

        return results;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Résout l'URL CardMarket depuis un TcgId ("base1-4") :
    ///   1. Recherche la carte + son set en BDD pour obtenir les noms.
    ///   2. Utilise cm_scraping_slug sur PokemonCard si présent (override manuel).
    ///   3. Sinon, génère le slug automatiquement depuis les noms.
    /// </summary>
    private async Task<string?> BuildUrlAsync(string tcgId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tcgId)) return null;

        // Récupérer la carte depuis la BDD
        PokemonCard? card = null;
        try
        {
            card = await _cardRepo.GetByTcgIdAsync(tcgId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Shutdown en cours — propager au lieu de traiter comme "carte introuvable"
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex, "CardMarketScraping — impossible de récupérer la carte '{TcgId}' depuis la BDD", tcgId);
        }

        if (card is null)
        {
            _logger.LogWarning(
                "CardMarketScraping — carte '{TcgId}' introuvable en BDD", tcgId);
            return null;
        }

        // Récupérer le set
        PokemonSet? set = null;
        try
        {
            set = await _setRepo.GetByIdAsync(card.SetId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex, "CardMarketScraping — impossible de récupérer le set pour '{TcgId}'", tcgId);
        }

        if (set is null)
        {
            _logger.LogWarning(
                "CardMarketScraping — set introuvable pour la carte '{TcgId}'", tcgId);
            return null;
        }

        // Override de slug manuel (cm_scraping_slug sur PokemonCard) ?
        var cardSlugOverride = card.CmScrapingSlug;

        var url = CardMarketScrapingUrlBuilder.BuildUrl(set.Name, card.Name, cardSlugOverride);

        if (url is null)
        {
            _logger.LogWarning(
                "CardMarketScraping — slug invalide pour set='{SetName}' card='{CardName}'",
                set.Name, card.Name);
        }

        return url;
    }

    private static CardPriceData MapToCardPriceData(CmScrapingResult r) =>
        new(
            Source:         "cardmarket-scraping",
            RawMarketPrice: r.TrendPrice ?? r.LowPrice,
            Psa7:           null,
            Psa8:           null,
            Psa9:           null,
            Psa10:          null,
            Currency:       "EUR",
            FetchedAt:      DateTime.UtcNow);
}

// ── DTO interne ───────────────────────────────────────────────────────────────

/// <summary>
/// Résultat brut extrait par Ollama depuis la page CardMarket.
/// Propriétés nullable : le LLM peut ne pas trouver certaines valeurs.
/// </summary>
internal sealed record CmScrapingResult(
    decimal? TrendPrice,
    decimal? LowPrice,
    decimal? Avg30);
