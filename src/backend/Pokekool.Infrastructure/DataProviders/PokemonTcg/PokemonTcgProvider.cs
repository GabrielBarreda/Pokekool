using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Application.DataCollection;

namespace Pokekool.Infrastructure.DataProviders.PokemonTcg;

/// <summary>
/// Provider PokemonTCG.io — implémente ICardMetadataProvider.
/// Délègue les appels HTTP à IPokemonTcgClient et normalise les résultats
/// en CardSetData / CardData indépendants de la source.
/// </summary>
internal sealed class PokemonTcgProvider : ICardMetadataProvider
{
    public string Name => "pokemontcg";
    public ProviderCapability Capabilities =>
        ProviderCapability.CardMetadata | ProviderCapability.MarketPrices;

    private readonly IPokemonTcgClient _client;
    private readonly ILogger<PokemonTcgProvider> _logger;

    public PokemonTcgProvider(IPokemonTcgClient client, ILogger<PokemonTcgProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CardSetData>> GetAllSetsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[{Provider}] GetAllSetsAsync", Name);
        try
        {
            var sets = await _client.GetAllSetsAsync(ct);
            return sets.Select(MapSet).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ProviderException(Name, $"Échec GetAllSetsAsync : {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<CardData>> GetCardsBySetAsync(
        string setTcgId, CancellationToken ct = default)
    {
        _logger.LogDebug("[{Provider}] GetCardsBySetAsync set={SetId}", Name, setTcgId);
        try
        {
            var cards = await _client.GetCardsBySetAsync(setTcgId, ct);
            return cards.Select(c => MapCard(c, setTcgId)).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ProviderException(Name, $"Échec GetCardsBySetAsync set='{setTcgId}' : {ex.Message}", ex);
        }
    }

    // ---- Mapping ----

    private static CardSetData MapSet(TcgSetDto s)
    {
        DateOnly? releaseDate = null;
        if (!string.IsNullOrWhiteSpace(s.ReleaseDate)
            && DateOnly.TryParseExact(s.ReleaseDate, "yyyy/MM/dd", null,
                System.Globalization.DateTimeStyles.None, out var parsed))
            releaseDate = parsed;

        return new CardSetData(
            ProviderSetId: s.Id,
            Name:          s.Name,
            Series:        s.Series,
            ReleaseDate:   releaseDate,
            TotalCards:    s.Total,
            SymbolUrl:     s.SymbolUrl,
            LogoUrl:       s.LogoUrl);
    }

    private static CardData MapCard(TcgCardDto c, string setTcgId)
        => new(
            ProviderCardId: c.Id,
            SetId:          setTcgId,
            Number:         c.Number,
            Name:           c.Name,
            Supertype:      c.Supertype,
            Subtypes:       c.Subtypes,
            Rarity:         c.Rarity,
            Types:          c.Types,
            Hp:             c.Hp,
            ImageSmall:     c.ImageSmall,
            ImageLarge:     c.ImageLarge,
            Prices:         MapPrice(c.Prices));

    /// <summary>
    /// Sélectionne le prix de référence par ordre de priorité :
    ///   1. TCGPlayer Normal Market (USD)
    ///   2. TCGPlayer Holofoil Market (USD)
    ///   3. TCGPlayer ReverseHolofoil Market (USD)
    ///   4. CardMarket Average Sell Price (EUR)
    /// </summary>
    private static CardPriceData? MapPrice(TcgCardPricesDto? prices)
    {
        if (prices is null) return null;

        if (prices.TcgPlayer is { } tcp)
        {
            var variant = tcp.Normal ?? tcp.Holofoil ?? tcp.ReverseHolofoil;
            if (variant?.Market is not null)
                return new CardPriceData(
                    Source:         "tcgplayer",
                    RawMarketPrice: variant.Market,
                    Psa7:  null, Psa8: null, Psa9: null, Psa10: null,
                    Currency:       "USD",
                    FetchedAt:      DateTime.UtcNow);
        }

        if (prices.CardMarket?.AverageSellPrice is { } avg)
            return new CardPriceData(
                Source:         "cardmarket",
                RawMarketPrice: avg,
                Psa7:  null, Psa8: null, Psa9: null, Psa10: null,
                Currency:       "EUR",
                FetchedAt:      DateTime.UtcNow);

        return null;
    }
}
