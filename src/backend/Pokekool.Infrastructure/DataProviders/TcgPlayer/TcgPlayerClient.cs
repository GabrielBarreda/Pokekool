using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Pokekool.Infrastructure.DataProviders.TcgPlayer;

/// <summary>
/// Client HTTP TCGPlayer v1.37.0.
/// L'auth Bearer est injectée automatiquement par TcgPlayerTokenHandler.
/// </summary>
internal sealed class TcgPlayerClient : ITcgPlayerClient
{
    // Limite d'IDs par appel /pricing/product selon la doc TCGPlayer
    private const int PricingBatchSize = 250;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient       _http;
    private readonly TcgPlayerOptions _opts;

    public TcgPlayerClient(HttpClient http, IOptions<TcgPlayerOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    // ── Sets (Groups) ─────────────────────────────────────────────────────────

    public async Task<List<TcgPlayerSet>> GetSetsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<TcgPlayerResponse<TcgPlayerSet>>(
            $"/catalog/categories/{_opts.CategoryId}/groups",
            JsonOptions, ct);

        return response?.Results ?? [];
    }

    // ── Produits ──────────────────────────────────────────────────────────────

    public async Task<List<TcgPlayerProduct>> GetProductsAsync(
        int groupId, CancellationToken ct = default)
    {
        var url = $"/catalog/products?categoryId={_opts.CategoryId}&groupId={groupId}&getExtendedFields=false";
        var response = await _http.GetFromJsonAsync<TcgPlayerResponse<TcgPlayerProduct>>(
            url, JsonOptions, ct);

        return response?.Results ?? [];
    }

    // ── Prix ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appelle GET /pricing/product/{ids} par lots de 250 (limite API TCGPlayer).
    /// Fusionne tous les résultats dans une seule liste.
    /// </summary>
    public async Task<List<TcgPlayerPrice>> GetPricesAsync(
        IEnumerable<int> productIds, CancellationToken ct = default)
    {
        var allIds  = productIds.Distinct().ToList();
        var results = new List<TcgPlayerPrice>();

        foreach (var batch in Chunk(allIds, PricingBatchSize))
        {
            ct.ThrowIfCancellationRequested();

            var idsStr   = string.Join(",", batch);
            var response = await _http.GetFromJsonAsync<TcgPlayerResponse<TcgPlayerPrice>>(
                $"/pricing/product/{idsStr}", JsonOptions, ct);

            if (response?.Results is not null)
                results.AddRange(response.Results);
        }

        return results;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
            yield return source.GetRange(i, Math.Min(size, source.Count - i));
    }
}
