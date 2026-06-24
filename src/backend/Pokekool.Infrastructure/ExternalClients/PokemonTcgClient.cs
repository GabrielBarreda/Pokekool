using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pokekool.Application.Abstractions;
using Pokekool.Infrastructure.Options;

namespace Pokekool.Infrastructure.ExternalClients;

/// <summary>
/// Client HTTP pour l'API PokémonTCG (https://api.pokemontcg.io/v2).
/// Gère la pagination automatique et le header X-Api-Key.
/// </summary>
internal sealed class PokemonTcgClient : IPokemonTcgClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<PokemonTcgClient> _logger;

    public PokemonTcgClient(HttpClient http, IOptions<PokemonTcgOptions> options, ILogger<PokemonTcgClient> logger)
    {
        _http = http;
        _logger = logger;

        var opts = options.Value;
        _http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + '/');

        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", opts.ApiKey);
    }

    public async Task<IReadOnlyList<TcgSetDto>> GetAllSetsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching all sets from PokemonTCG API...");

        var response = await _http.GetAsync("sets", ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonSerializer.Deserialize<TcgEnvelope<TcgSetResponse>>(content, JsonOptions);

        if (envelope?.Data is null)
            return [];

        return envelope.Data
            .Select(s => new TcgSetDto(
                s.Id,
                s.Name,
                s.Series,
                s.ReleaseDate,
                s.Total,
                s.PrintedTotal,
                s.Images?.Symbol,
                s.Images?.Logo))
            .ToList();
    }

    public async Task<IReadOnlyList<TcgCardDto>> GetCardsBySetAsync(string setId, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching cards for set '{SetId}' from PokemonTCG API...", setId);

        const int pageSize = 250;
        var page = 1;
        var allCards = new List<TcgCardDto>();

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var url = $"cards?q=set.id:{setId}&pageSize={pageSize}&page={page}";
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var envelope = JsonSerializer.Deserialize<TcgEnvelope<TcgCardResponse>>(content, JsonOptions);

            if (envelope?.Data is null || envelope.Data.Count == 0)
                break;

            foreach (var c in envelope.Data)
            {
                allCards.Add(new TcgCardDto(
                    c.Id,
                    c.Name,
                    c.Number,
                    c.Supertype,
                    c.Subtypes,
                    c.Rarity,
                    c.Types,
                    int.TryParse(c.Hp, out var hp) ? hp : null,
                    c.Images?.Small,
                    c.Images?.Large,
                    MapPrices(c)));
            }

            _logger.LogDebug("Set '{SetId}': fetched page {Page} ({Count} cards).", setId, page, envelope.Data.Count);

            if (envelope.Data.Count < pageSize)
                break;

            page++;
        }

        _logger.LogInformation("Set '{SetId}': total {Total} cards fetched.", setId, allCards.Count);
        return allCards;
    }

    // ---- Price mapping ----

    private static TcgCardPricesDto? MapPrices(TcgCardResponse c)
    {
        var hasTcp = c.TcgPlayer?.Prices is not null;
        var hasCm = c.CardMarket?.Prices is not null;
        if (!hasTcp && !hasCm) return null;

        TcgPlayerPricesDto? tcgPlayer = null;
        if (c.TcgPlayer?.Prices is { } tcp)
        {
            tcgPlayer = new TcgPlayerPricesDto(
                MapVariant(tcp.Normal),
                MapVariant(tcp.Holofoil),
                MapVariant(tcp.ReverseHolofoil));
        }

        CardMarketPricesDto? cardMarket = null;
        if (c.CardMarket?.Prices is { } cm)
        {
            cardMarket = new CardMarketPricesDto(cm.AverageSellPrice, cm.LowPrice);
        }

        return new TcgCardPricesDto(tcgPlayer, cardMarket);
    }

    private static TcgPriceVariantDto? MapVariant(TcgVariantResponse? v)
        => v is null ? null : new TcgPriceVariantDto(v.Low, v.Mid, v.High, v.Market);

    // ---- Internal deserialization models ----

    private sealed class TcgEnvelope<T>
    {
        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }
    }

    private sealed class TcgSetResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("series")]
        public string? Series { get; set; }

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }

        [JsonPropertyName("printedTotal")]
        public int? PrintedTotal { get; set; }

        [JsonPropertyName("images")]
        public TcgSetImages? Images { get; set; }
    }

    private sealed class TcgSetImages
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
    }

    private sealed class TcgCardResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("number")]
        public string Number { get; set; } = default!;

        [JsonPropertyName("supertype")]
        public string? Supertype { get; set; }

        [JsonPropertyName("subtypes")]
        public string[]? Subtypes { get; set; }

        [JsonPropertyName("rarity")]
        public string? Rarity { get; set; }

        [JsonPropertyName("types")]
        public string[]? Types { get; set; }

        [JsonPropertyName("hp")]
        public string? Hp { get; set; }

        [JsonPropertyName("images")]
        public TcgCardImages? Images { get; set; }

        [JsonPropertyName("tcgplayer")]
        public TcgPlayerResponse? TcgPlayer { get; set; }

        [JsonPropertyName("cardmarket")]
        public CardMarketResponse? CardMarket { get; set; }
    }

    private sealed class TcgCardImages
    {
        [JsonPropertyName("small")]
        public string? Small { get; set; }

        [JsonPropertyName("large")]
        public string? Large { get; set; }
    }

    private sealed class TcgPlayerResponse
    {
        [JsonPropertyName("prices")]
        public TcgPlayerPricesResponse? Prices { get; set; }
    }

    private sealed class TcgPlayerPricesResponse
    {
        [JsonPropertyName("normal")]
        public TcgVariantResponse? Normal { get; set; }

        [JsonPropertyName("holofoil")]
        public TcgVariantResponse? Holofoil { get; set; }

        [JsonPropertyName("reverseHolofoil")]
        public TcgVariantResponse? ReverseHolofoil { get; set; }
    }

    private sealed class TcgVariantResponse
    {
        [JsonPropertyName("low")]
        public decimal? Low { get; set; }

        [JsonPropertyName("mid")]
        public decimal? Mid { get; set; }

        [JsonPropertyName("high")]
        public decimal? High { get; set; }

        [JsonPropertyName("market")]
        public decimal? Market { get; set; }
    }

    private sealed class CardMarketResponse
    {
        [JsonPropertyName("prices")]
        public CardMarketPricesResponse? Prices { get; set; }
    }

    private sealed class CardMarketPricesResponse
    {
        [JsonPropertyName("averageSellPrice")]
        public decimal? AverageSellPrice { get; set; }

        [JsonPropertyName("lowPrice")]
        public decimal? LowPrice { get; set; }
    }
}
