namespace Pokekool.Application.DataCollection;

/// <summary>
/// Abstraction d'un scraper web (Playwright ou autre).
/// </summary>
public interface IWebScraper
{
    /// <summary>
    /// Récupère le HTML brut d'une URL via un navigateur headless.
    /// </summary>
    /// <param name="url">URL cible.</param>
    /// <param name="options">Options de scraping (wait, blocage ressources, headers…).</param>
    /// <param name="ct">Token d'annulation.</param>
    /// <returns>HTML brut ou null si échec.</returns>
    Task<string?> FetchPageHtmlAsync(string url, ScraperOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Navigue vers l'URL, attend le chargement, puis évalue une fonction JS
    /// et désérialise le résultat en <typeparamref name="T"/>.
    /// Contourne totalement Ollama — pour les sites server-rendered à DOM stable.
    /// </summary>
    /// <param name="jsFunction">
    /// Fonction JS autonome <c>() =&gt; ({ ... })</c> retournant un objet sérialisable.
    /// </param>
    Task<T?> EvaluateJsAsync<T>(
        string url,
        string jsFunction,
        ScraperOptions? options = null,
        CancellationToken ct = default) where T : class;
}

/// <summary>
/// Options de configuration du scraper pour un appel donné.
/// </summary>
public sealed record ScraperOptions(
    int WaitForNetworkIdleMs = 2000,
    string? WaitForSelector = null,
    bool BlockImages = true,
    bool BlockFonts = true,
    Dictionary<string, string>? Headers = null);
