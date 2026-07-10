using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Pokekool.Application.DataCollection;

namespace Pokekool.Infrastructure.DataProviders.Scraping;

/// <summary>
/// Scraper web basé sur Playwright (Chromium headless).
/// L'instance de browser est réutilisée (Singleton) pour éviter la surcharge de création.
///
/// Stealth anti-bot (couche 1) :
///   • --disable-blink-features=AutomationControlled → supprime la bannière "Chrome est contrôlé"
///   • AddInitScript : masque navigator.webdriver, injecte window.chrome, plugins, languages
///   • User-Agent + viewport aléatoires à chaque context
///   • Accept-Language FR par défaut
///   • Délai court aléatoire avant navigation (simulate think time)
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
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15"
    ];

    // Script injecté dans chaque page AVANT le chargement du site.
    // Masque les indicateurs d'automatisation les plus détectés (couche 1 Cloudflare/PerimeterX).
    private const string StealthInitScript = """
        // 1. Masquer navigator.webdriver (flag le plus vérifié par les anti-bots)
        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });

        // 2. Simuler window.chrome présent dans tout vrai Chrome
        if (!window.chrome) {
            window.chrome = {
                runtime: {},
                loadTimes: () => ({}),
                csi: () => ({}),
                app: {}
            };
        }

        // 3. Plugins non vides (headless Chrome = 0 plugins, suspect)
        Object.defineProperty(navigator, 'plugins', {
            get: () => Object.assign([], { length: 3 })
        });

        // 4. Langue FR cohérente avec le User-Agent
        Object.defineProperty(navigator, 'languages', {
            get: () => ['fr-FR', 'fr', 'en-US', 'en']
        });

        // 5. Permission notifications = 'default' (headless retourne souvent 'denied' d'emblée)
        const origQuery = window.navigator.permissions?.query?.bind(window.navigator.permissions);
        if (origQuery) {
            window.navigator.permissions.query = (params) =>
                params.name === 'notifications'
                    ? Promise.resolve({ state: 'default', onchange: null })
                    : origQuery(params);
        }
        """;

    // Viewports représentatifs — évite le 1280×720 par défaut qui crie "bot"
    private static readonly (int W, int H)[] Viewports =
    [
        (1920, 1080), (1536, 864), (1440, 900),
        (1366, 768),  (1280, 800), (1280, 720)
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

        await using var context = await CreateStealthContextAsync(options);
        var page = await SetupPageAsync(context, options);

        // Délai pré-navigation : simule le think time humain (évite les requêtes trop uniformes)
        await Task.Delay(Random.Shared.Next(300, 900), ct);

        var waitUntil = options.WaitForNetworkIdle
            ? WaitUntilState.NetworkIdle
            : WaitUntilState.DOMContentLoaded;

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = waitUntil, Timeout = 30_000 });

        if (options.WaitForNetworkIdleMs > 0)
            await page.WaitForTimeoutAsync(options.WaitForNetworkIdleMs);

        if (options.WaitForSelector is not null)
        {
            try
            {
                await page.WaitForSelectorAsync(options.WaitForSelector,
                    new PageWaitForSelectorOptions { Timeout = 10_000 });
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("EvaluateJs — WaitForSelector '{Sel}' timeout pour {Url}",
                    options.WaitForSelector, url);
            }
        }

        // Vérification anti-blocage : un HTML trop court = challenge page, pas de contenu réel
        var htmlLen = await page.EvaluateAsync<int>("() => document.documentElement.outerHTML.length");
        if (htmlLen < 2_000)
        {
            _logger.LogWarning("EvaluateJs — HTML suspect ({Len} chars) pour {Url} — probable blocage bot",
                htmlLen, url);
            return null;
        }

        try
        {
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
        await using var context = await CreateStealthContextAsync(options);
        var page = await SetupPageAsync(context, options);

        await Task.Delay(Random.Shared.Next(300, 900), ct);

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });

        if (options.WaitForSelector is not null)
            await page.WaitForSelectorAsync(options.WaitForSelector, new PageWaitForSelectorOptions { Timeout = 10_000 });

        if (options.WaitForNetworkIdleMs > 0)
            await page.WaitForTimeoutAsync(options.WaitForNetworkIdleMs);

        return await page.ContentAsync();
    }

    /// <summary>
    /// Crée un context Playwright avec profil stealth complet :
    /// User-Agent + viewport aléatoires, Accept-Language FR, init script anti-détection.
    /// </summary>
    private async Task<IBrowserContext> CreateStealthContextAsync(ScraperOptions options)
    {
        var ua = UserAgents[Random.Shared.Next(UserAgents.Length)];
        var vp = Viewports[Random.Shared.Next(Viewports.Length)];

        var extraHeaders = new Dictionary<string, string>(options.Headers ?? [])
        {
            // Accept-Language cohérent avec la locale FR
            ["Accept-Language"] = "fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7"
        };

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent        = ua,
            ViewportSize     = new ViewportSize { Width = vp.W, Height = vp.H },
            Locale           = "fr-FR",
            TimezoneId       = "Europe/Paris",
            ExtraHTTPHeaders = extraHeaders
        });

        // Script stealth injecté avant tout JS de la page cible
        await context.AddInitScriptAsync(StealthInitScript);

        return context;
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
                Args =
                [
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    // Supprime la bannière "Chrome est piloté par un logiciel de test automatisé"
                    // et le flag interne AutomationControlled détecté par certains anti-bots
                    "--disable-blink-features=AutomationControlled",
                    // Désactive l'extension d'automatisation Chromium (lève navigator.webdriver)
                    "--disable-automation",
                    // Réduit l'empreinte GPU/WebGL souvent fingerprinted
                    "--disable-gpu",
                    "--disable-software-rasterizer"
                ]
            });

            _initialized = true;
            _logger.LogInformation("Playwright Chromium initialisé (stealth activé)");
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
