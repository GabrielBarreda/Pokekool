using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.DataProviders.GemTracker;

/// <summary>
/// Provider de population GemTracker (gemtracker.co) — supporte PSA + BGS.
///
/// Stratégie :
///   • Scrape par SET (pas par carte) : 1-2 pages Playwright par set (une par société).
///   • Les résultats sont mis en cache en mémoire pour la durée de la sync.
///   • Le mapping set (era / set_slug / set_id) est lu depuis provider_set_mappings.
///   • Si aucun mapping n'existe : tentative de découverte automatique par fuzzy match sur le nom.
///
/// URL : https://gemtracker.co/en/pokemon/en/{era}/{set-slug}/{set-id}/cards/{company}/population-report
/// </summary>
public sealed class GemTrackerProvider : IPopulationProvider, IDisposable
{
    public string Name => "gemtracker";
    public ProviderCapability Capabilities => ProviderCapability.Population;

    private const string BaseUrl     = "https://gemtracker.co";
    // Page de liste des sets anglais (Next.js App Router, locale /fr/ mais langue /en/)
    private const string SetsListUrl = "https://gemtracker.co/fr/pokemon/en/liste-des-extensions-du-jcc-pokemon-en-anglais";

    private static readonly string[] ScrapedCompanies = ["psa", "bgs"];

    // Options pour les pages de population d'un set (colonnes React, ~169 cartes)
    private static readonly ScraperOptions SpaOptions = new(
        WaitForNetworkIdleMs: 4000,
        WaitForSelector:      null,
        BlockImages:          true,
        BlockFonts:           true,
        WaitForNetworkIdle:   true);

    // Regex pour extraire era/slug/id depuis le HTML SSR de la page liste des sets
    // ex: /pokemon/en/wotc/jungle/20002
    private static readonly Regex SetLinkPattern = new(
        @"/pokemon/en/([^/""\s]+)/([^/""\s]+)/(\d+)",
        RegexOptions.Compiled);

    // ── JS : extraction de la matrice de population (composant React column-based) ──
    // Structure : bodyStickyLeft > colNumero (169 cells) + bodyScroller > bodyRow (12 grade cols × 169 cells)
    // Colonnes grade : AUTH(0) | 1(1)…10(10) | Total(11)
    // Plusieurs variantes par numéro → on agrège (somme) par numéro de carte
    private const string PopTableJs = """
        () => {
            const p = 'PopulationMatrixStickyJS-module__4ChR8a__';
            const stickyLeft  = document.querySelector(`[class*="${p}bodyStickyLeft"]`);
            const bodyScroll  = document.querySelector(`[class*="${p}bodyScroller"]`);
            if (!stickyLeft || !bodyScroll) return [];
            const bodyRow = bodyScroll.querySelector(`[class*="${p}bodyRow"]`);
            if (!bodyRow) return [];
            const colNumero = stickyLeft.querySelector(`[class*="${p}colNumero"]`);
            if (!colNumero) return [];
            const gradeCols = Array.from(bodyRow.children);
            if (gradeCols.length < 12) return [];
            const parseNum = s => { const n = parseInt((s || '').replace(/[^\d]/g, '')); return isNaN(n) ? 0 : n; };
            const numCells = Array.from(colNumero.children);
            const byNumber = {};
            for (let i = 0; i < numCells.length; i++) {
                const number = numCells[i].textContent.trim().replace(/^#/, '');
                if (!number || !/^\d/.test(number)) continue;
                const pop10 = parseNum(gradeCols[10]?.children[i]?.textContent);
                const pop9  = parseNum(gradeCols[9]?.children[i]?.textContent);
                const pop8  = parseNum(gradeCols[8]?.children[i]?.textContent);
                const pop7  = parseNum(gradeCols[7]?.children[i]?.textContent);
                const total = parseNum(gradeCols[11]?.children[i]?.textContent);
                if (!byNumber[number]) byNumber[number] = { number, pop10: 0, pop9: 0, pop8: 0, pop7: 0, total: 0 };
                byNumber[number].pop10 += pop10;
                byNumber[number].pop9  += pop9;
                byNumber[number].pop8  += pop8;
                byNumber[number].pop7  += pop7;
                byNumber[number].total += total;
            }
            return Object.values(byNumber);
        }
        """;


    // ── Cache mémoire (durée d'une sync) ─────────────────────────────────────
    // key: setId → Task<cardNumber → list of CardPopulationData par société>
    private readonly ConcurrentDictionary<Guid, Lazy<Task<IReadOnlyDictionary<string, IReadOnlyList<CardPopulationData>>>>> _setCache = new();

    // Cache de mapping set : évite les appels DB redondants pour le même setId
    private readonly ConcurrentDictionary<Guid, Task<GemTrackerSetMapping?>> _mappingCache = new();

    // Sérialise tous les accès DB (DbContext non thread-safe) dans ce provider
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);

    private readonly IWebScraper                    _scraper;
    private readonly IProviderMappingRepository     _mappingRepo;
    private readonly IHttpClientFactory             _httpClientFactory;
    private readonly ILogger<GemTrackerProvider>    _logger;
    private readonly Random                         _rng = new();

    public GemTrackerProvider(
        IWebScraper                 scraper,
        IProviderMappingRepository  mappingRepo,
        IHttpClientFactory          httpClientFactory,
        ILogger<GemTrackerProvider> logger)
    {
        _scraper           = scraper;
        _mappingRepo       = mappingRepo;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    // ── IPopulationProvider ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<CardPopulationData>?> GetPopulationAsync(
        CardPopulationRequest request, CancellationToken ct = default)
    {
        // 1. Résoudre le mapping set (une seule requête DB par setId, même en parallèle)
        var mapping = await _mappingCache.GetOrAdd(
            request.SetId,
            id => GetOrResolveSetMappingAsync(id, request.SetName, ct));
        if (mapping is null)
        {
            _logger.LogDebug(
                "GemTracker — aucun mapping pour le set '{SetName}' ({SetId}), carte ignorée",
                request.SetName, request.SetId);
            return null;
        }

        // 2. Obtenir (ou lancer) le scrape du set — partagé entre toutes les cartes du même set
        var lazy = _setCache.GetOrAdd(
            request.SetId,
            _ => new Lazy<Task<IReadOnlyDictionary<string, IReadOnlyList<CardPopulationData>>>>(
                     () => ScrapeSetAsync(mapping, ct)));

        IReadOnlyDictionary<string, IReadOnlyList<CardPopulationData>> setData;
        try
        {
            setData = await lazy.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "GemTracker — échec scrape pour le set '{SetName}'", request.SetName);
            return null;
        }

        // 3. Rechercher par numéro de carte
        if (!setData.TryGetValue(request.CardNumber, out var results))
        {
            _logger.LogDebug(
                "GemTracker — carte #{Number} '{Name}' absente des données du set '{SetName}'",
                request.CardNumber, request.CardName, request.SetName);
            return null;
        }

        return results;
    }

    // ── Résolution du mapping set ─────────────────────────────────────────────

    private async Task<GemTrackerSetMapping?> GetOrResolveSetMappingAsync(
        Guid setId, string setName, CancellationToken ct)
    {
        // _dbSemaphore garantit qu'un seul accès DB a lieu à la fois (DbContext non thread-safe)
        await _dbSemaphore.WaitAsync(ct);
        string? era, slug, gemId;
        try
        {
            era   = await _mappingRepo.GetSetValueAsync(setId, ProviderKeys.GemTracker, ProviderKeys.Era, ct);
            slug  = await _mappingRepo.GetSetValueAsync(setId, ProviderKeys.GemTracker, ProviderKeys.GemSetSlug, ct);
            gemId = await _mappingRepo.GetSetValueAsync(setId, ProviderKeys.GemTracker, ProviderKeys.GemSetId, ct);
        }
        finally { _dbSemaphore.Release(); }

        if (era is not null && slug is not null && gemId is not null)
            return new GemTrackerSetMapping(era, slug, gemId);

        // Pas de mapping → tentative de découverte automatique
        _logger.LogInformation(
            "GemTracker — aucun mapping pour '{SetName}', tentative de découverte automatique",
            setName);

        return await DiscoverAndStoreSetMappingAsync(setId, setName, ct);
    }

    private async Task<GemTrackerSetMapping?> DiscoverAndStoreSetMappingAsync(
        Guid setId, string setName, CancellationToken ct)
    {
        // La page liste est SSR : les 328 liens sont dans le HTML brut, pas besoin de JS/Playwright
        string html;
        try
        {
            var http = _httpClientFactory.CreateClient("gemtracker");
            html = await http.GetStringAsync(SetsListUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GemTracker — impossible de charger la liste des sets (HTTP)");
            return null;
        }

        // Extraire chaque triplet era/slug/id depuis les hrefs
        var seen    = new HashSet<string>();
        var entries = new List<GemTrackerSetEntry>();
        foreach (Match m in SetLinkPattern.Matches(html))
        {
            if (!seen.Add(m.Value)) continue;
            // Le slug est le nom normalisé : "base-set" → "base set" pour le fuzzy match
            var nameFromSlug = m.Groups[2].Value.Replace("-", " ");
            entries.Add(new GemTrackerSetEntry(nameFromSlug, m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value));
        }

        if (entries.Count == 0)
        {
            _logger.LogWarning("GemTracker — liste des sets vide (HTML {Len} chars)", html.Length);
            return null;
        }

        _logger.LogDebug("GemTracker — {Count} sets trouvés dans le HTML SSR", entries.Count);

        var match = entries.FirstOrDefault(e => FuzzyMatch(setName, e.Name));
        if (match is null)
        {
            _logger.LogWarning(
                "GemTracker — aucun match pour '{SetName}' parmi {Count} sets",
                setName, entries.Count);
            return null;
        }

        _logger.LogInformation(
            "GemTracker — match trouvé : '{SetName}' → era={Era} slug={Slug} id={Id}",
            setName, match.Era, match.Slug, match.GemId);

        // Stocker le mapping pour les syncs suivantes (accès DB sérialisé)
        await _dbSemaphore.WaitAsync(ct);
        try
        {
            await _mappingRepo.UpsertSetMappingAsync(setId, ProviderKeys.GemTracker, ProviderKeys.Era,        match.Era,   ct);
            await _mappingRepo.UpsertSetMappingAsync(setId, ProviderKeys.GemTracker, ProviderKeys.GemSetSlug, match.Slug,  ct);
            await _mappingRepo.UpsertSetMappingAsync(setId, ProviderKeys.GemTracker, ProviderKeys.GemSetId,   match.GemId, ct);
        }
        finally { _dbSemaphore.Release(); }

        return new GemTrackerSetMapping(match.Era, match.Slug, match.GemId);
    }

    // ── Scrape d'un set complet (PSA + BGS) ──────────────────────────────────

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<CardPopulationData>>> ScrapeSetAsync(
        GemTrackerSetMapping mapping, CancellationToken ct)
    {
        // cardNumber → liste des données par société
        var combined = new Dictionary<string, List<CardPopulationData>>(StringComparer.OrdinalIgnoreCase);

        foreach (var company in ScrapedCompanies)
        {
            if (ct.IsCancellationRequested) break;

            var url = $"{BaseUrl}/fr/pokemon/en/{mapping.Era}/{mapping.SetSlug}/{mapping.GemSetId}/carte/{company}/rapport-de-population";
            _logger.LogInformation("GemTracker — scrape {Url}", url);

            GemTrackerPopRow[]? rows;
            try
            {
                rows = await _scraper.EvaluateJsAsync<GemTrackerPopRow[]>(url, PopTableJs, SpaOptions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GemTracker — échec scrape {Company} pour set {Slug}", company, mapping.SetSlug);
                rows = null;
            }

            if (rows is null || rows.Length == 0)
            {
                _logger.LogDebug("GemTracker — aucune donnée {Company} pour set {Slug}", company, mapping.SetSlug);
            }
            else
            {
                var source = $"gemtracker-{company}";
                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.Number)) continue;

                    double? gemRate = row.Total > 0 && row.Pop10.HasValue
                        ? Math.Round((double)row.Pop10.Value / row.Total.Value * 100, 2)
                        : null;

                    var data = new CardPopulationData(
                        Source:    source,
                        Total:     row.Total,
                        Pop10:     row.Pop10,
                        Pop9:      row.Pop9,
                        Pop8:      row.Pop8,
                        Pop7:      row.Pop7,
                        GemRate10: gemRate,
                        FetchedAt: DateTime.UtcNow);

                    if (!combined.TryGetValue(row.Number, out var list))
                        combined[row.Number] = list = [];

                    list.Add(data);
                }

                _logger.LogInformation(
                    "GemTracker — {Company} set '{Slug}' : {Count} cartes extraites",
                    company.ToUpperInvariant(), mapping.SetSlug, rows.Length);
            }

            // Rate limiting entre les deux pages du set
            await Task.Delay(_rng.Next(1500, 3001), ct);
        }

        return combined.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<CardPopulationData>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    // ── Fuzzy match ───────────────────────────────────────────────────────────

    private static bool FuzzyMatch(string dbName, string gemName)
    {
        static string Normalize(string s) =>
            new string(
                s.ToLowerInvariant()
                 .Replace("é", "e").Replace("è", "e").Replace("ê", "e")
                 .Replace("à", "a").Replace("â", "a")
                 .Replace("ô", "o").Replace("ù", "u").Replace("û", "u")
                 .Replace("&", "and").Replace("-", " ")
                 .Where(c => char.IsLetterOrDigit(c) || c == ' ')
                 .ToArray())
            .Trim();

        var a = Normalize(dbName);
        var b = Normalize(gemName);

        return a == b
            || a.Contains(b, StringComparison.Ordinal)
            || b.Contains(a, StringComparison.Ordinal);
    }

    public void Dispose() => _dbSemaphore.Dispose();
}

// ── DTOs internes ─────────────────────────────────────────────────────────────

internal sealed record GemTrackerSetMapping(string Era, string SetSlug, string GemSetId);

internal sealed record GemTrackerSetEntry(string Name, string Era, string Slug, string GemId);

internal sealed record GemTrackerPopRow(
    string? Number,
    int?    Pop10,
    int?    Pop9,
    int?    Pop8,
    int?    Pop7,
    int?    Total);
