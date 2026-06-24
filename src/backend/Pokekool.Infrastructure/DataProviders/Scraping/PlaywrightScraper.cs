using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Pokekool.Application.DataCollection;

namespace Pokekool.Infrastructure.DataProviders.Scraping;

/// <summary>
/// Scraper web basé sur Playwright (Chromium headless).
/// L'instance de browser est réutilisée (Singleton) pour éviter la surcharge de création.
/// </summary>
public sealed class PlaywrightScraper : IWebScraper, IAsyncDisposable
{
    private readonly ILogger<PlaywrightScraper> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    private const int MaxRetries = 2;

    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15"
    ];

    public PlaywrightScraper(ILogger<PlaywrightScraper> logger)
    {
        _logger = logger;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // ── IWebScraper : FetchPageHtmlAsync ─────────────────────────────────────

    public async Task<string?> FetchPageHtmlAsync(string url, ScraperOptions? options = null, CancellationToken ct = default)
    {
        options ??= new ScraperOptions();
        await EnsureInitializedAsync(ct);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try { return await FetchAsync(url, options, ct); }
            catch (TimeoutException ex) when (attempt < MaxRetries)
                { _logger.LogWarning(ex, "Timeout Playwright (tentative {A}/{M}) pour {Url}", attempt, MaxRetries, url); }
            catch (PlaywrightException ex) when (attempt < MaxRetries)
                { _logger.LogWarning(ex, "Erreur Playwright (tentative {A}/{M}) pour {Url}", attempt, MaxRetries, url); }
        }

        try { return await FetchAsync(url, options, ct); }
        catch (Exception ex) { _logger.LogError(ex, "Échec définitif du scraping pour {Url}", url); return null; }
    }

    // ── IWebScraper : EvaluateJsAsync ────────────────────────────────────────

    public async Task<T?> EvaluateJsAsync<T>(
        string url,
        string jsFunction,
        ScraperOptions? options = null,
        CancellationToken ct = default) where T : class
    {
        options ??= new ScraperOptions();
        await EnsureInitializedAsync(ct);

        var userAgent = UserAgents[Random.Shared.Next(UserAgents.Length)];
        var contextOptions = new BrowserNewContextOptions
        {
            UserAgent  = userAgent,
            ExtraHTTPHeaders = options.Headers ?? new Dictionary<string, string>()
        };

        await using var context = await _browser!.NewContextAsync(contextOptions);
        var page = await SetupPageAsync(context, options);

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });

        if (options.WaitForNetworkIdleMs > 0)
            await page.WaitForTimeoutAsync(options.WaitForNetworkIdleMs);

        try
        {
            // On enveloppe jsFunction dans JSON.stringify pour récupérer un string C#
            var json = await page.EvaluateAsync<string?>($"() => JSON.stringify(({jsFunction})())");
            if (json is null) return null;
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EvaluateJs — échec évaluation JS pour {Url}", url);
            return null;
        }
    }

    // ── Helpers privés ────────────────────────────────────────────────────────

    private async Task<string?> FetchAsync(string url, ScraperOptions options, CancellationToken ct)
    {
        var userAgent = UserAgents[Random.Shared.Next(UserAgents.Length)];
        var contextOptions = new BrowserNewContextOptions
        {
            UserAgent  = userAgent,
            ExtraHTTPHeaders = options.Headers ?? new Dictionary<string, string>()
        };

        await using var context = await _browser!.NewContextAsync(contextOptions);
        var page = await SetupPageAsync(context, options);

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });

        if (options.WaitForSelector is not null)
            await page.WaitForSelectorAsync(options.WaitForSelector, new PageWaitForSelectorOptions { Timeout = 10_000 });

        if (options.WaitForNetworkIdleMs > 0)
            await page.WaitForTimeoutAsync(options.WaitForNetworkIdleMs);

        return await page.ContentAsync();
    }

    /// <summary>Crée une page avec le blocage de ressources configuré.</summary>
    private static async Task<IPage> SetupPageAsync(IBrowserContext context, ScraperOptions options)
    {
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", async route =>
        {
            var rt = route.Request.ResourceType;
            if ((options.BlockImages && rt == "image") ||
                (options.BlockFonts  && rt == "font")  ||
                rt == "media")
                await route.AbortAsync();
            else
                await route.ContinueAsync();
        });
        return page;
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
            });

            _initialized = true;
            _logger.LogInformation("Playwright Chromium initialisé");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();
        _initLock.Dispose();
    }
}
