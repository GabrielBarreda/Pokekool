using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Chat;
using Pokekool.Application.DataCollection;

namespace Pokekool.Infrastructure.Chat;

/// <summary>
/// Recherche de prix marché via scraping des ventes terminées eBay FR (IWebScraper / Playwright existant).
/// Dégradation gracieuse : tout échec retourne null.
/// </summary>
public sealed class EbayMarketPriceResearcher : IMarketPriceResearcher
{
    private readonly IWebScraper _scraper;
    private readonly ILogger<EbayMarketPriceResearcher> _logger;

    // Prix du style "12,50 EUR" ou "12.50 EUR".
    private static readonly Regex PriceRegex = new(
        @"(\d{1,6}[,\.]\d{2})\s*EUR",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public EbayMarketPriceResearcher(IWebScraper scraper, ILogger<EbayMarketPriceResearcher> logger)
    {
        _scraper = scraper;
        _logger = logger;
    }

    public async Task<MarketPriceResult?> SearchEbayRecentSalesAsync(string productName, CancellationToken ct)
    {
        try
        {
            var encoded = Uri.EscapeDataString(productName);
            var url = $"https://www.ebay.fr/sch/i.html?_nkw={encoded}&LH_Sold=1&LH_Complete=1&_sop=13&LH_ItemCondition=3";

            var html = await _scraper.FetchPageHtmlAsync(url, new ScraperOptions(
                WaitForNetworkIdleMs: 1500,
                BlockImages: true,
                BlockFonts: true,
                WaitForNetworkIdle: false), ct);

            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("eBay ventes terminées : HTML vide pour '{Product}'", productName);
                return null;
            }

            var matches = PriceRegex.Matches(html);
            if (matches.Count == 0)
                return null;

            var prices = matches
                .Select(m => m.Groups[1].Value.Replace(',', '.'))
                .Select(s => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
                .Where(d => d.HasValue && d > 0.5m)
                .Select(d => d!.Value)
                .OrderBy(d => d)
                .ToList();

            if (prices.Count == 0)
                return null;

            // Retirer les outliers (top 10% et bottom 10%).
            var trimCount = Math.Max(0, (int)(prices.Count * 0.10));
            var trimmed = prices.Skip(trimCount).Take(prices.Count - 2 * trimCount).ToList();
            if (trimmed.Count == 0) trimmed = prices;

            return new MarketPriceResult(
                ProductName: productName,
                AveragePrice: Math.Round(trimmed.Average(), 2),
                MinPrice: trimmed.Min(),
                MaxPrice: trimmed.Max(),
                SampleCount: trimmed.Count,
                Currency: "EUR",
                RetrievedAt: DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur recherche prix eBay pour '{Product}'", productName);
            return null;
        }
    }
}
