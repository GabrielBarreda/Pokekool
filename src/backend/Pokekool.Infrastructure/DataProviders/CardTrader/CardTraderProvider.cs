using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.DataProviders.CardTrader;

/// <summary>
/// Provider CardTrader — prix de marché EUR, extraction CSS directe (sans Ollama).
///
/// Pipeline : Playwright → EvaluateJsAsync → CardTraderResult → CardPriceData
///
/// Source de données : https://www.cardtrader.com (scraping public, aucune clé requise)
/// Marché principalement EUR — complément utile à PriceCharting (USD) pour la moyenne pondérée.
///
/// Le JS injecté tente plusieurs stratégies dans l'ordre :
///   1. Recherche d'un élément "Market Price" explicite (donnée officielle CardTrader)
///   2. Recherche du prix "Starting From" (listing le moins cher disponible)
///   3. Scan de tous les éléments de prix et retour du minimum (fallback)
/// </summary>
public sealed class CardTraderProvider : ICardPriceProvider, IDisposable
{
    public string Name => "cardtrader";
    public ProviderCapability Capabilities => ProviderCapability.MarketPrices;

    // ── Extraction JS (Playwright, pas d'Ollama) ─────────────────────────────
    //
    // CardTrader (SPA JS-rendered) affiche sur ses pages marketplace :
    //   • "Market price" : prix moyen de marché calculé par CardTrader
    //   • "Starting from" : listing le moins cher du moment
    //
    // Stratégie 1 — label "Market price" ou "Market Price" près d'un montant
    // Stratégie 2 — label "Starting from" ou "starting-from" près d'un montant
    // Stratégie 3 — scanle texte de la page pour format "$X" ou "X€" et retourne le minimum
    //               (évite de retourner une valeur d'expédition ou de taxe aberrante)
    //
    // NOTE : les sélecteurs CSS exacts dépendent du build SPA → affiner après premier test réel.
    private const string CssExtractionJs = """
        () => {
            const parsePrice = (text) => {
                if (!text) return null;
                // Formats : "$12.50", "12,50 €", "12.50€", "€12.50"
                const m = text.match(/(?:\$|€)\s*([0-9,]+\.?\d*)|([0-9,]+\.?\d*)\s*(?:€|\$)/);
                if (!m) return null;
                const raw = (m[1] ?? m[2]).replace(/,/g, '');
                const val = parseFloat(raw);
                return isNaN(val) || val <= 0 ? null : val;
            };

            const detectCurrency = (text) => /€/.test(text) ? 'EUR' : 'USD';

            // ── Stratégie 1 : "Market price" label ───────────────────────────
            const MARKET_LABELS = ['market price', 'market-price', 'prix marché', 'marktpreis'];
            for (const label of MARKET_LABELS) {
                const el = Array.from(document.querySelectorAll('*'))
                    .find(e => e.children.length <= 2
                        && e.textContent?.toLowerCase().includes(label));
                if (el) {
                    // Prix souvent dans un sibling ou enfant suivant
                    const candidates = [
                        el.nextElementSibling,
                        el.parentElement?.nextElementSibling,
                        el.querySelector('[class*="price"],[class*="value"],[class*="amount"]'),
                        el.closest('[class*="price"],[class*="market"]'),
                    ].filter(Boolean);

                    for (const c of candidates) {
                        const price = parsePrice(c?.textContent);
                        if (price) return { marketPrice: price, lowestPrice: null, currency: detectCurrency(c.textContent) };
                    }
                    // Fallback : le prix est peut-être dans le même élément
                    const inEl = parsePrice(el.textContent);
                    if (inEl) return { marketPrice: inEl, lowestPrice: null, currency: detectCurrency(el.textContent) };
                }
            }

            // ── Stratégie 2 : "Starting from" label ──────────────────────────
            const FROM_LABELS = ['starting from', 'starting-from', 'à partir de', 'ab '];
            for (const label of FROM_LABELS) {
                const el = Array.from(document.querySelectorAll('*'))
                    .find(e => e.children.length <= 2
                        && e.textContent?.toLowerCase().startsWith(label));
                if (el) {
                    const price = parsePrice(el.nextElementSibling?.textContent ?? el.textContent);
                    if (price) return { marketPrice: null, lowestPrice: price, currency: detectCurrency(el.textContent) };
                }
            }

            // ── Stratégie 3 : scan complet, prix minimum raisonnable ──────────
            const allPrices = [];
            const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
            let node;
            while ((node = walker.nextNode())) {
                const text = node.textContent?.trim();
                if (!text || text.length > 30) continue;
                const price = parsePrice(text);
                // Filtre anti-bruit : entre 0.01€ et 100k€ (évite TVA, frais de port hors-norme)
                if (price && price >= 0.01 && price <= 100_000) allPrices.push(price);
            }

            if (allPrices.length === 0) return { marketPrice: null, lowestPrice: null, currency: 'EUR' };

            allPrices.sort((a, b) => a - b);
            return {
                marketPrice: null,
                lowestPrice: allPrices[0],
                currency: 'EUR',
            };
        }
        """;

    private static readonly ScraperOptions PlaywrightOptions = new(
        WaitForNetworkIdleMs: 4000,
        WaitForSelector:      null,
        BlockImages:          true,
        BlockFonts:           true,
        Headers: new Dictionary<string, string>
        {
            ["Accept-Language"] = "fr-FR,fr;q=0.9,en;q=0.8",
        });

    private readonly IWebScraper                    _scraper;
    private readonly IPokemonCardRepository         _cardRepo;
    private readonly IPokemonSetRepository          _setRepo;
    private readonly ILogger<CardTraderProvider>    _logger;
    private readonly Random                         _rng      = new();
    private readonly SemaphoreSlim                  _dbSemaphore;

    public CardTraderProvider(
        IWebScraper                  scraper,
        IPokemonCardRepository       cardRepo,
        IPokemonSetRepository        setRepo,
        ILogger<CardTraderProvider>  logger,
        DbConcurrencyGuard           dbGuard)
    {
        _scraper     = scraper;
        _cardRepo    = cardRepo;
        _setRepo     = setRepo;
        _logger      = logger;
        _dbSemaphore = dbGuard.Semaphore;
    }

    public void Dispose() { /* _dbSemaphore appartient au DbConcurrencyGuard scoped */ }

    // ── ICardPriceProvider ────────────────────────────────────────────────────

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

        _logger.LogInformation("CardTrader CSS — fetch {Url}", url);

        try
        {
            var raw = await _scraper.EvaluateJsAsync<CardTraderResult>(
                url, CssExtractionJs, PlaywrightOptions, ct);

            // On préfère marketPrice (calculé par CardTrader) sinon lowestPrice (listing live)
            var price = raw?.MarketPrice ?? raw?.LowestPrice;
            if (price is null or 0)
            {
                _logger.LogWarning("CardTrader CSS — aucune donnée pour '{Id}'", cardIdentifier);
                return null;
            }

            _logger.LogDebug(
                "CardTrader CSS — '{Id}' : market={Market} lowest={Lowest} currency={Currency}",
                cardIdentifier, raw!.MarketPrice, raw.LowestPrice, raw.Currency);

            return new CardPriceData(
                Source:         "cardtrader",
                RawMarketPrice: price,
                Currency:       raw.Currency ?? "EUR",
                FetchedAt:      DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CardTrader CSS — erreur scraping pour '{Id}'", cardIdentifier);
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

            try
            {
                var price = await GetPriceAsync(id, ct);
                if (price is not null) results.Add(price);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CardTrader CSS — erreur ignorée pour '{Id}'", id);
            }

            await Task.Delay(_rng.Next(2000, 4001), ct);
        }

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(string? CardName, string? SetName)> ResolveNamesAsync(
        string tcgId, CancellationToken ct)
    {
        await _dbSemaphore.WaitAsync(ct);
        try
        {
            var card = await _cardRepo.GetByTcgIdAsync(tcgId, ct);
            if (card is null) return (null, null);

            var lastDash  = tcgId.LastIndexOf('-');
            var setPrefix = lastDash > 0 ? tcgId[..lastDash] : tcgId;
            var set       = await _setRepo.GetByTcgIdAsync(setPrefix, ct);

            return (card.Name, set?.Name);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CardTrader — DB lookup échoué pour '{Id}'", tcgId);
            return (null, null);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    // ── DTO interne ───────────────────────────────────────────────────────────

    private sealed record CardTraderResult(
        decimal? MarketPrice,
        decimal? LowestPrice,
        string?  Currency);
}
