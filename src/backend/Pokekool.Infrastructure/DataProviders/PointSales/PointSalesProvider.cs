using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.DataProviders.PointSales;

/// <summary>
/// Provider 130point.com — ventes eBay complétées, extraction CSS directe (sans Ollama).
///
/// Pipeline : Playwright → EvaluateJsAsync → PointSalesResult → CardPriceData
///
/// Le JS injecté dans Playwright :
///   1. Parcourt tous les éléments de vente de la page
///   2. Extrait le couple (titre, prix) de chaque vente
///   3. Classe chaque vente par grade (PSA 1-10, brut) via regex sur le titre
///   4. Calcule la moyenne par grade — évite de mélanger raw/gradé dans un seul chiffre
///   5. Déduplique les entrées par (titre normalisé + prix) pour éviter les doublons d'affichage
///
/// Source de données : https://130point.com (scraping public, aucune clé requise)
/// </summary>
public sealed class PointSalesProvider : ICardPriceProvider, IDisposable
{
    public string Name => "130point";
    public ProviderCapability Capabilities =>
        ProviderCapability.MarketPrices | ProviderCapability.SalesHistory | ProviderCapability.GradedPrices;

    // ── Extraction JS (Playwright, pas d'Ollama) ─────────────────────────────
    //
    // Stratégie :
    //  • Cherche d'abord des conteneurs structurés (.ebay-result, .result, .sold-item, etc.)
    //  • Chaque conteneur → titre + prix extraits
    //  • Fallback : TreeWalker sur les text nodes contenant "$X.XX"
    //  • Grade détecté dans le titre via regex PSA/BGS/CGC/SGC
    //  • Déduplication par (prix + snippet titre) pour éviter les doublons de rendu
    //  • Moyenne par grade : raw (aucun organisme mentionné) + PSA1-10
    private const string CssExtractionJs = """
        () => {
            const parsePrice = (text) => {
                if (!text) return null;
                const m = text.match(/\$\s*([0-9,]+\.?\d*)/);
                return m ? parseFloat(m[1].replace(/,/g, '')) : null;
            };

            // Détecte la présence d'un organisme de notation dans le titre
            const GRADING_RE = /\b(?:psa|bgs|cgc|sgc|ace|hga|gma)\s*\d/i;
            const hasGrade = (t) => GRADING_RE.test(t);

            const sales = [];
            const seen  = new Set();

            // ── Stratégie 1 : conteneurs structurés ────────────────────────
            const CONTAINERS = [
                '.ebay-result', '.result-item', '.sold-item', '.sale-item',
                '.listing-item', '[class*="result"]', '[class*="listing"]',
                'table tbody tr', '.table-row'
            ];

            for (const sel of CONTAINERS) {
                const els = Array.from(document.querySelectorAll(sel));
                for (const el of els) {
                    // Ignorer les wrappers trop larges
                    const children = el.querySelectorAll('*').length;
                    if (children > 50) continue;

                    const price = parsePrice(el.textContent);
                    if (!price || price < 0.5 || price > 5_000_000) continue;

                    const titleEl = el.querySelector('a, h3, h4, h5, .title, [class*="title"], [class*="name"]');
                    const title   = (titleEl?.textContent ?? el.textContent).trim().slice(0, 120);
                    if (!title) continue;

                    const key = `${Math.round(price * 100)}_${title.slice(0, 40).toLowerCase().replace(/\s+/g, '')}`;
                    if (seen.has(key)) continue;
                    seen.add(key);

                    sales.push({ title, price });
                }
                if (sales.length >= 5) break; // Stratégie 1 a fonctionné
            }

            // ── Stratégie 2 : TreeWalker fallback ───────────────────────────
            if (sales.length === 0) {
                const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
                let node;
                while ((node = walker.nextNode())) {
                    const text = node.textContent.trim();
                    if (!/^\$[0-9,]+\.?\d*$/.test(text)) continue;

                    const price = parsePrice(text);
                    if (!price || price < 0.5 || price > 5_000_000) continue;

                    const parent  = node.parentElement;
                    const context = parent?.closest('tr, li, [class*="item"], [class*="result"]');
                    const title   = (context?.textContent ?? parent?.textContent ?? '').trim().slice(0, 120);

                    const key = `${Math.round(price * 100)}_${title.slice(0, 40).toLowerCase().replace(/\s+/g, '')}`;
                    if (seen.has(key)) continue;
                    seen.add(key);

                    sales.push({ title, price });
                }
            }

            // ── Classification par grade + société ───────────────────────────
            const extractGradeInfo = (title) => {
                // Extrait (société, note) depuis le titre — ex: "PSA 10", "BGS 9.5"
                const m = title.match(/\b(psa|bgs|cgc|sgc|ace|hga|gma)\s*([\d.]+)/i);
                if (!m) return null;
                return { company: m[1].toUpperCase(), grade: parseFloat(m[2]) };
            };

            const gradeGroups = {};   // key: "PSA_10", value: { company, grade, prices[] }
            const rawPrices   = [];

            for (const { title, price } of sales) {
                if (!hasGrade(title)) { rawPrices.push(price); continue; }
                const info = extractGradeInfo(title);
                if (!info) continue;
                const key = `${info.company}_${info.grade}`;
                if (!gradeGroups[key]) gradeGroups[key] = { company: info.company, grade: info.grade, prices: [] };
                gradeGroups[key].prices.push(price);
            }

            const avg  = (arr) => arr.length === 0 ? null : Math.round(arr.reduce((a,b) => a+b, 0) / arr.length * 100) / 100;
            const last = (arr) => arr.length === 0 ? null : arr[arr.length - 1];

            const grades = Object.values(gradeGroups)
                .map(g => ({ company: g.company, grade: g.grade, price: avg(g.prices) }))
                .filter(g => g.price !== null);

            return {
                averagePrice:  avg(rawPrices),
                lastSalePrice: last(rawPrices),
                grades,
                salesCount: sales.length
            };
        }
        """;

    private static readonly ScraperOptions PlaywrightOptions = new(
        WaitForNetworkIdleMs:  1500,    // délai supplémentaire après NetworkIdle (rendu)
        WaitForSelector:       null,
        BlockImages:           true,
        BlockFonts:            true,
        WaitForNetworkIdle:    true,    // attend que les XHR soient terminés (résultats eBay)
        Headers: new Dictionary<string, string>
        {
            ["Accept-Language"] = "en-US,en;q=0.9",
        });

    private readonly IWebScraper                 _scraper;
    private readonly IPokemonCardRepository      _cardRepo;
    private readonly IPokemonSetRepository       _setRepo;
    private readonly ILogger<PointSalesProvider> _logger;
    private readonly Random                      _rng      = new();

    // Sémaphore partagé via DbConcurrencyGuard (scoped)
    private readonly SemaphoreSlim _dbSemaphore;

    public PointSalesProvider(
        IWebScraper                 scraper,
        IPokemonCardRepository      cardRepo,
        IPokemonSetRepository       setRepo,
        ILogger<PointSalesProvider> logger,
        DbConcurrencyGuard          dbGuard)
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
            _logger.LogWarning("130point — noms introuvables pour '{Id}'", cardIdentifier);
            return null;
        }

        // Extraire le numéro de carte depuis le TcgId (ex: "base1-4" → "4")
        var cardNumber = ExtractCardNumber(cardIdentifier);

        var url = PointSalesUrlBuilder.BuildUrl(cardName, setName, cardNumber);
        if (url is null) return null;

        _logger.LogInformation("130point CSS — fetch {Url}", url);

        try
        {
            var raw = await _scraper.EvaluateJsAsync<PointSalesResult>(
                url, CssExtractionJs, PlaywrightOptions, ct);

            // On accepte le résultat dès qu'il y a au moins une donnée utile
            if (raw is null || (raw.AveragePrice is null && (raw.Grades is null || raw.Grades.Count == 0)))
            {
                _logger.LogWarning(
                    "130point CSS — aucune donnée extraite pour '{Id}' (salesCount={Count})",
                    cardIdentifier, raw?.SalesCount ?? 0);
                return null;
            }

            _logger.LogDebug(
                "130point CSS — '{Id}' : raw={Raw} grades={GradeCount} sales={Count}",
                cardIdentifier, raw.AveragePrice, raw.Grades?.Count ?? 0, raw.SalesCount);

            return MapToCardPriceData(raw);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "130point CSS — erreur scraping pour '{Id}'", cardIdentifier);
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
                _logger.LogWarning(ex, "130point CSS — erreur ignorée pour '{Id}' dans le batch", id);
            }

            // Rate limiting : 2–4 s entre requêtes pour éviter le rate-limit côté 130point
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
            _logger.LogDebug(ex, "130point — DB lookup échoué pour '{Id}'", tcgId);
            return (null, null);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    /// <summary>Extrait le numéro de carte depuis un TcgId de type "base1-4" → "4".</summary>
    private static string? ExtractCardNumber(string tcgId)
    {
        var lastDash = tcgId.LastIndexOf('-');
        return lastDash >= 0 ? tcgId[(lastDash + 1)..] : null;
    }

    private static CardPriceData MapToCardPriceData(PointSalesResult r)
    {
        var grades = r.Grades?
            .Select(g => new GradedPriceData(g.Company, g.Grade, g.Price))
            .ToList();

        return new CardPriceData(
            Source:         "130point",
            RawMarketPrice: r.AveragePrice ?? r.LastSalePrice,
            GradedPrices:   grades?.Count > 0 ? grades : null,
            Currency:       "USD",
            FetchedAt:      DateTime.UtcNow);
    }

    // ── DTOs internes ─────────────────────────────────────────────────────────

    private sealed record PointSalesGrade(string Company, decimal Grade, decimal Price);

    private sealed record PointSalesResult(
        decimal?                        AveragePrice,
        decimal?                        LastSalePrice,
        IReadOnlyList<PointSalesGrade>? Grades,
        int?                            SalesCount);
}
