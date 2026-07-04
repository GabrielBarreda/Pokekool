using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.DataProviders.Scraping;

namespace Pokekool.Infrastructure.DataProviders.PriceCharting;

/// <summary>
/// Provider PriceCharting — prix bruts et gradés (PSA/CGC).
///
/// Mode d'extraction configurable via le paramètre "extraction_mode" :
///   • "css"   (défaut) : évaluation JS dans Playwright — lecture directe du DOM.
///                        0 CPU Ollama, ~2-3 s par carte.
///   • "ollama"          : pipeline Playwright → HtmlCleaner → Ollama (fallback LLM).
/// </summary>
public sealed class PriceChartingProvider : ICardPriceProvider, IDisposable
{
    public string Name => "pricecharting";
    public ProviderCapability Capabilities => ProviderCapability.MarketPrices | ProviderCapability.GradedPrices;

    // ── Extraction CSS : fonction JS évaluée par Playwright ──────────────────
    //
    // Lit la table de prix PriceCharting par en-têtes textuels (résilient
    // aux changements de layout mineur). Retourne { rawPrice, psa7…psa10 }.
    private const string CssExtractionJs = """
        () => {
            const parsePrice = (text) => {
                if (!text) return null;
                const m = text.match(/\$([0-9,]+\.?\d*)/);
                return m ? parseFloat(m[1].replace(/,/g, '')) : null;
            };

            const tables = Array.from(document.querySelectorAll('table'));
            for (const table of tables) {
                const headers = Array.from(table.querySelectorAll('th'))
                                     .map(th => th.textContent.trim());
                if (!headers.some(h => /ungraded/i.test(h))) continue;

                const rows = Array.from(table.querySelectorAll('tr'))
                                  .filter(r => r.querySelectorAll('td').length > 2);
                for (const row of rows) {
                    const cells = Array.from(row.querySelectorAll('td'));
                    const idx   = (pred) => headers.findIndex(pred);
                    const get   = (pred) => { const i = idx(pred); return i >= 0 && cells[i] ? parsePrice(cells[i].textContent) : null; };

                    const result = {
                        rawPrice : get(h => /ungraded/i.test(h)),
                        psa1     : get(h => /^grade\s*1$/i.test(h)),
                        psa2     : get(h => /^grade\s*2$/i.test(h)),
                        psa3     : get(h => /^grade\s*3$/i.test(h)),
                        psa4     : get(h => /^grade\s*4$/i.test(h)),
                        psa5     : get(h => /^grade\s*5$/i.test(h)),
                        psa6     : get(h => /^grade\s*6$/i.test(h)),
                        psa7     : get(h => /^grade\s*7$/i.test(h)),
                        psa8     : get(h => /^grade\s*8$/i.test(h)),
                        psa9     : get(h => /^grade\s*9$/i.test(h)),
                        psa10    : get(h => /psa\s*10/i.test(h))
                    };

                    if (result.rawPrice !== null || result.psa10 !== null) return result;
                }
            }

            return { rawPrice: null, psa1: null, psa2: null, psa3: null, psa4: null, psa5: null, psa6: null, psa7: null, psa8: null, psa9: null, psa10: null };
        }
        """;

    // ── Schéma JSON pour le mode Ollama (fallback) ───────────────────────────
    private static readonly string OllamaJsonSchema =
        """
        {
          "rawPrice": 0.0,
          "psa7": 0.0,
          "psa8": 0.0,
          "psa9": 0.0,
          "psa10": 0.0,
          "cgc9": 0.0,
          "cgc10": 0.0
        }
        """;

    private static readonly ScraperOptions PlaywrightOptions = new(
        WaitForNetworkIdleMs: 2000,
        WaitForSelector:      null,
        BlockImages:          true,
        BlockFonts:           true);

    private readonly IWebScraper                    _scraper;
    private readonly ScrapingPipeline               _pipeline;
    private readonly IPokemonCardRepository         _cardRepo;
    private readonly IPokemonSetRepository          _setRepo;
    private readonly IProviderSettingsService       _settings;
    private readonly IProviderMappingRepository     _mappingRepo;
    private readonly ILogger<PriceChartingProvider> _logger;
    private readonly Random _rng = new();

    // Thread-safety : sérialise les accès DB (non thread-safe avec EF Core scoped DbContext)
    // + cache des données set/mode pour éviter les requêtes répétées
    private readonly SemaphoreSlim _dbSemaphore;
    private readonly ConcurrentDictionary<string, (string? Slug, string? Name)> _setCache = new();
    private string? _cachedMode;

    public PriceChartingProvider(
        IWebScraper                    scraper,
        ScrapingPipeline               pipeline,
        IPokemonCardRepository         cardRepo,
        IPokemonSetRepository          setRepo,
        IProviderSettingsService       settings,
        IProviderMappingRepository     mappingRepo,
        ILogger<PriceChartingProvider> logger,
        DbConcurrencyGuard             dbGuard)
    {
        _scraper      = scraper;
        _pipeline     = pipeline;
        _cardRepo     = cardRepo;
        _setRepo      = setRepo;
        _settings     = settings;
        _mappingRepo  = mappingRepo;
        _logger       = logger;
        _dbSemaphore  = dbGuard.Semaphore;
    }

    public void Dispose() { /* _dbSemaphore appartient au DbConcurrencyGuard scoped — pas à disposer ici */ }

    // ── ICardPriceProvider ────────────────────────────────────────────────────

    public async Task<CardPriceData?> GetPriceAsync(
        string cardIdentifier, CancellationToken ct = default)
    {
        var (url, mode) = await BuildUrlAsync(cardIdentifier, ct);
        if (url is null)
        {
            _logger.LogWarning("PriceCharting — impossible de construire l'URL pour '{Id}'", cardIdentifier);
            return null;
        }

        _logger.LogInformation("PriceCharting [{Mode}] — fetch {Url}", mode, url);

        try
        {
            return mode == "ollama"
                ? await FetchWithOllamaAsync(url, cardIdentifier, ct)
                : await FetchWithCssAsync(url, cardIdentifier, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PriceCharting — erreur scraping pour '{Id}'", cardIdentifier);
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

            var price = await GetPriceAsync(id, ct);
            if (price is not null)
                results.Add(price);

            await Task.Delay(_rng.Next(1000, 3001), ct);
        }

        return results;
    }

    // ── Extraction CSS ────────────────────────────────────────────────────────

    private async Task<CardPriceData?> FetchWithCssAsync(
        string url, string cardId, CancellationToken ct)
    {
        var raw = await _scraper.EvaluateJsAsync<PriceChartingResult>(
            url, CssExtractionJs, PlaywrightOptions, ct);

        if (raw is null || (raw.RawPrice is null && raw.Psa10 is null))
        {
            _logger.LogWarning("PriceCharting CSS — aucune donnée extraite pour '{Id}'", cardId);
            return null;
        }

        _logger.LogDebug(
            "PriceCharting CSS — '{Id}' : raw={Raw} psa10={Psa10}",
            cardId, raw.RawPrice, raw.Psa10);

        return MapToCardPriceData(raw);
    }

    // ── Extraction Ollama (fallback) ──────────────────────────────────────────

    private async Task<CardPriceData?> FetchWithOllamaAsync(
        string url, string cardId, CancellationToken ct)
    {
        var raw = await _pipeline.ExecuteAsync<PriceChartingResult>(
            url, OllamaJsonSchema, PlaywrightOptions, ct);

        if (raw is null) return null;

        _logger.LogDebug(
            "PriceCharting Ollama — '{Id}' : raw={Raw} psa10={Psa10}",
            cardId, raw.RawPrice, raw.Psa10);

        return MapToCardPriceData(raw);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Résout URL + mode d'extraction depuis la DB et les settings.</summary>
    /// <remarks>
    /// Thread-safe : les accès DB sont sérialisés via _dbSemaphore (EF Core DbContext non thread-safe).
    /// Les résultats set + mode sont mis en cache pour éviter les requêtes répétées.
    /// </remarks>
    private async Task<(string? Url, string Mode)> BuildUrlAsync(
        string tcgId, CancellationToken ct)
    {
        var lastDash = tcgId.LastIndexOf('-');
        if (lastDash <= 0) return (null, "css");

        var setPrefix = tcgId[..lastDash];

        string? dbSlug   = null;
        string? setName  = null;
        string? cardName = null;
        string  mode     = "css";

        // Cache hit pour le set → pas besoin d'aller en DB
        if (_setCache.TryGetValue(setPrefix, out var cached))
        {
            (dbSlug, setName) = cached;
        }

        // Besoin d'aller en DB (set non mis en cache ou cardName à résoudre)
        bool needsDb = !_setCache.ContainsKey(setPrefix) || _cachedMode is null;
        if (needsDb)
        {
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                // Re-vérifier après acquisition du lock (un autre thread a peut-être déjà chargé)
                if (!_setCache.TryGetValue(setPrefix, out cached))
                {
                    try
                    {
                        var set = await _setRepo.GetByTcgIdAsync(setPrefix, ct);
                        string? slug = set is not null
                            ? await _mappingRepo.GetSetValueAsync(set.Id, ProviderKeys.PriceCharting, ProviderKeys.Slug, ct)
                            : null;
                        cached = (slug, set?.Name);
                        _setCache[setPrefix] = cached;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "PriceCharting — set '{Prefix}' non résolu depuis la BDD", setPrefix);
                        _setCache[setPrefix] = (null, null);
                    }
                }
                (dbSlug, setName) = cached;

                if (_cachedMode is null)
                {
                    try   { _cachedMode = await _settings.GetAsync("pricecharting", "extraction_mode", ct) ?? "css"; }
                    catch { _cachedMode = "css"; }
                }

                // cardName : unique par carte, pas mis en cache (résolu dans le lock)
                try
                {
                    var card = await _cardRepo.GetByTcgIdAsync(tcgId, ct);
                    cardName = card?.Name;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "PriceCharting — carte '{TcgId}' non résolue depuis la BDD", tcgId);
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        else
        {
            // Set et mode déjà en cache — résoudre uniquement cardName (dans le lock)
            await _dbSemaphore.WaitAsync(ct);
            try
            {
                try
                {
                    var card = await _cardRepo.GetByTcgIdAsync(tcgId, ct);
                    cardName = card?.Name;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "PriceCharting — carte '{TcgId}' non résolue depuis la BDD", tcgId);
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        mode = _cachedMode ?? "css";

        var url = PriceChartingUrlBuilder.BuildUrl(tcgId, cardName, dbSlug, setName);
        if (url is null)
            _logger.LogDebug(
                "PriceCharting — aucun slug set trouvé pour '{Prefix}' (setName='{SetName}')",
                setPrefix, setName);

        return (url, mode);
    }

    private static CardPriceData MapToCardPriceData(PriceChartingResult r)
    {
        // PriceCharting labels toutes ses colonnes "Grade X" sans nommer la société.
        // Dans les faits, seuls les grades PSA sont tracés sur ce site.
        var grades = new List<GradedPriceData>();
        if (r.Psa1  is not null) grades.Add(new("PSA", 1m,  r.Psa1.Value));
        if (r.Psa2  is not null) grades.Add(new("PSA", 2m,  r.Psa2.Value));
        if (r.Psa3  is not null) grades.Add(new("PSA", 3m,  r.Psa3.Value));
        if (r.Psa4  is not null) grades.Add(new("PSA", 4m,  r.Psa4.Value));
        if (r.Psa5  is not null) grades.Add(new("PSA", 5m,  r.Psa5.Value));
        if (r.Psa6  is not null) grades.Add(new("PSA", 6m,  r.Psa6.Value));
        if (r.Psa7  is not null) grades.Add(new("PSA", 7m,  r.Psa7.Value));
        if (r.Psa8  is not null) grades.Add(new("PSA", 8m,  r.Psa8.Value));
        if (r.Psa9  is not null) grades.Add(new("PSA", 9m,  r.Psa9.Value));
        if (r.Psa10 is not null) grades.Add(new("PSA", 10m, r.Psa10.Value));

        return new CardPriceData(
            Source:         "pricecharting",
            RawMarketPrice: r.RawPrice,
            GradedPrices:   grades.Count > 0 ? grades : null,
            Currency:       "USD",
            FetchedAt:      DateTime.UtcNow);
    }

    // ── DTO interne ───────────────────────────────────────────────────────────

    private sealed record PriceChartingResult(
        decimal? RawPrice,
        decimal? Psa1,
        decimal? Psa2,
        decimal? Psa3,
        decimal? Psa4,
        decimal? Psa5,
        decimal? Psa6,
        decimal? Psa7,
        decimal? Psa8,
        decimal? Psa9,
        decimal? Psa10,
        decimal? Cgc9,
        decimal? Cgc10);
}
