using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Pokekool.Infrastructure.DataProviders.CardMarket;

/// <summary>
/// Client HTTP CardMarket v2.0.
/// L'auth OAuth 1.0a est injectée automatiquement par CardMarketAuthHandler.
/// </summary>
internal sealed class CardMarketClient : ICardMarketClient
{
    private readonly HttpClient _http;
    private readonly CardMarketOptions _opts;

    public CardMarketClient(HttpClient http, IOptions<CardMarketOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    // ── Expansion (sets) ───────────────────────────────────────────────────

    /// <summary>Liste toutes les expansions Pokémon. Utilisé pour le mapping TCG↔CM.</summary>
    public async Task<List<CmExpansion>> GetExpansionsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<CmExpansionsResponse>(
            $"/expansions/{_opts.PokemonGameId}", ct);
        return response?.Expansions ?? [];
    }

    // ── Singles (cartes d'une expansion) ──────────────────────────────────

    /// <summary>
    /// Retourne toutes les cartes (singles) d'une expansion CM avec leur price guide.
    /// </summary>
    public async Task<List<CmSingle>> GetSinglesAsync(int idExpansion, CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<CmSinglesResponse>(
            $"/expansions/{idExpansion}/singles", ct);
        return response?.Singles ?? [];
    }

    // ── Test connexion ─────────────────────────────────────────────────────

    /// <summary>Appel léger pour valider que les credentials sont corrects.</summary>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            // Endpoint account — retourne 200 si credentials valides
            var resp = await _http.GetAsync($"/account", ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
