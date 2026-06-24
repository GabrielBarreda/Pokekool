using Microsoft.Extensions.Logging;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.DataCollection;

/// <summary>
/// Orchestre la collecte de données via les providers enregistrés.
/// L'application appelle l'Aggregator — jamais les providers directement.
/// </summary>
public sealed class DataAggregator
{
    private readonly IEnumerable<ICardMetadataProvider> _metadataProviders;
    private readonly IEnumerable<ICardPriceProvider> _priceProviders;
    private readonly IPokemonSeriesRepository _seriesRepo;
    private readonly IPokemonSetRepository _setRepo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardMarketPriceRepository _priceRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DataAggregator> _logger;

    public DataAggregator(
        IEnumerable<ICardMetadataProvider> metadataProviders,
        IEnumerable<ICardPriceProvider> priceProviders,
        IPokemonSeriesRepository seriesRepo,
        IPokemonSetRepository setRepo,
        IPokemonCardRepository cardRepo,
        ICardMarketPriceRepository priceRepo,
        IUnitOfWork uow,
        ILogger<DataAggregator> logger)
    {
        _metadataProviders = metadataProviders;
        _priceProviders    = priceProviders;
        _seriesRepo        = seriesRepo;
        _setRepo           = setRepo;
        _cardRepo          = cardRepo;
        _priceRepo         = priceRepo;
        _uow               = uow;
        _logger            = logger;
    }

    /// <summary>
    /// Synchronise le catalogue complet (séries, sets, cartes, prix embarqués).
    /// Utilise le premier ICardMetadataProvider disponible (priorité : "pokemontcg").
    /// Idempotent : relancer ne crée pas de doublons.
    /// </summary>
    public async Task<SyncCatalogResult> SyncCatalogAsync(
        bool forceRefresh = false, CancellationToken ct = default)
    {
        var provider = SelectMetadataProvider();
        if (provider is null)
            return new SyncCatalogResult(0, 0, 0, 0, ["Aucun ICardMetadataProvider enregistré."]);

        _logger.LogInformation(
            "SyncCatalog démarré via le provider '{Provider}'. ForceRefresh={ForceRefresh}",
            provider.Name, forceRefresh);

        var errors = new List<string>();
        int seriesUpserted = 0, setsUpserted = 0, cardsUpserted = 0, pricesUpserted = 0;

        // Charger les séries existantes en cache local
        var existingSeriesAll = await _seriesRepo.ListAsync(ct);
        var seriesByName = existingSeriesAll
            .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<CardSetData> sets;
        try
        {
            sets = await provider.GetAllSetsAsync(ct);
        }
        catch (ProviderException ex)
        {
            _logger.LogError(ex, "Provider '{Provider}' a échoué sur GetAllSetsAsync.", ex.ProviderName);
            return new SyncCatalogResult(0, 0, 0, 0, [ex.Message]);
        }

        foreach (var setData in sets)
        {
            ct.ThrowIfCancellationRequested();

            // ---- Upsert Series ----
            var seriesName = string.IsNullOrWhiteSpace(setData.Series) ? "Unknown" : setData.Series.Trim();
            if (!seriesByName.TryGetValue(seriesName, out var seriesEntity))
            {
                var seriesTcgId = seriesName.ToLowerInvariant().Replace(" ", "-");
                seriesEntity = PokemonSeries.Create(seriesTcgId, seriesName, null);
                await _seriesRepo.AddAsync(seriesEntity, ct);
                seriesByName[seriesName] = seriesEntity;
                seriesUpserted++;
                _logger.LogDebug("Série importée : {Series}", seriesName);
            }

            // ---- Upsert Set ----
            var existingSet = await _setRepo.GetByTcgIdAsync(setData.ProviderSetId, ct);

            PokemonSet setEntity;
            if (existingSet is null)
            {
                setEntity = PokemonSet.Create(
                    setData.ProviderSetId,
                    seriesEntity.Id,
                    setData.Name,
                    seriesName,
                    setData.ReleaseDate,
                    setData.TotalCards,
                    printedTotal: null,
                    setData.SymbolUrl,
                    setData.LogoUrl);
                await _setRepo.AddAsync(setEntity, ct);
                setsUpserted++;
                _logger.LogDebug("Set importé : {Set}", setData.Name);
            }
            else
            {
                setEntity = existingSet;
                setEntity.Update(
                    setData.Name,
                    setData.ReleaseDate,
                    setData.TotalCards,
                    printedTotal: null,
                    setData.SymbolUrl,
                    setData.LogoUrl);
            }

            // Sauvegarder série + set avant les cartes (clés étrangères)
            await _uow.SaveChangesAsync(ct);

            // ---- Skip si déjà synchronisé et pas de force ----
            if (!forceRefresh && existingSet is not null && existingSet.Cards.Count > 0)
            {
                _logger.LogDebug("Set {SetId} déjà synchronisé, skip.", setData.ProviderSetId);
                continue;
            }

            // ---- Upsert Cards ----
            IReadOnlyList<CardData> cards;
            try
            {
                cards = await provider.GetCardsBySetAsync(setData.ProviderSetId, ct);
            }
            catch (ProviderException ex)
            {
                var msg = $"Provider '{ex.ProviderName}' a échoué sur set '{setData.ProviderSetId}' : {ex.Message}";
                _logger.LogWarning(ex, "{Message}", msg);
                errors.Add(msg);
                continue;
            }

            var newCards     = new List<PokemonCard>();
            var pricesToSave = new List<CardMarketPrice>();

            foreach (var cardData in cards)
            {
                PokemonCard cardEntity;
                var existing = await _cardRepo.GetByTcgIdAsync(cardData.ProviderCardId, ct);

                if (existing is null)
                {
                    cardEntity = PokemonCard.Create(
                        cardData.ProviderCardId,
                        setEntity.Id,
                        cardData.Number,
                        cardData.Name,
                        cardData.Supertype,
                        cardData.Subtypes,
                        cardData.Rarity,
                        cardData.Types,
                        cardData.Hp,
                        cardData.ImageSmall,
                        cardData.ImageLarge);
                    newCards.Add(cardEntity);
                    cardsUpserted++;
                }
                else
                {
                    existing.Update(
                        cardData.Name,
                        cardData.Supertype,
                        cardData.Subtypes,
                        cardData.Rarity,
                        cardData.Types,
                        cardData.Hp,
                        cardData.ImageSmall,
                        cardData.ImageLarge);
                    cardEntity = existing;
                    cardsUpserted++;
                }

                if (cardData.Prices is { } priceData)
                    pricesToSave.Add(MapToMarketPrice(cardEntity.Id, priceData));
            }

            if (newCards.Count > 0)
                await _cardRepo.AddRangeAsync(newCards, ct);

            await _uow.SaveChangesAsync(ct);

            if (pricesToSave.Count > 0)
            {
                await _priceRepo.UpsertBatchAsync(pricesToSave, ct);
                await _uow.SaveChangesAsync(ct);
                pricesUpserted += pricesToSave.Count;
            }

            _logger.LogInformation(
                "Set {SetId} : {Cards} cartes, {Prices} prix.",
                setData.ProviderSetId, cards.Count, pricesToSave.Count);
        }

        _logger.LogInformation(
            "SyncCatalog terminé. Séries={S} Sets={Se} Cartes={C} Prix={P} Erreurs={E}",
            seriesUpserted, setsUpserted, cardsUpserted, pricesUpserted, errors.Count);

        return new SyncCatalogResult(seriesUpserted, setsUpserted, cardsUpserted, pricesUpserted, errors);
    }

    /// <summary>
    /// Met à jour les prix des cartes indiquées en interrogeant tous les ICardPriceProvider enregistrés.
    /// Merge : on garde le prix le plus récent par (source, variant).
    /// </summary>
    public async Task<SyncPricesResult> SyncPricesAsync(
        IEnumerable<Guid> cardIds, CancellationToken ct = default)
    {
        var ids = cardIds.ToList();
        var errors = new List<string>();
        int pricesUpserted = 0, cardsFailed = 0;

        var providers = _priceProviders.ToList();
        if (providers.Count == 0)
        {
            _logger.LogWarning("SyncPricesAsync appelé mais aucun ICardPriceProvider enregistré.");
            return new SyncPricesResult(0, 0, ["Aucun ICardPriceProvider enregistré."]);
        }

        // Résoudre TcgId pour chaque carte
        var cardIdToTcgId = new Dictionary<Guid, string>();
        foreach (var cardId in ids)
        {
            var card = await _cardRepo.GetByIdAsync(cardId, ct);
            if (card is null)
            {
                errors.Add($"Carte introuvable : {cardId}");
                cardsFailed++;
                continue;
            }
            cardIdToTcgId[cardId] = card.TcgId;
        }

        foreach (var provider in providers)
        {
            var toUpsert = new List<CardMarketPrice>();

            foreach (var (cardId, tcgId) in cardIdToTcgId)
            {
                CardPriceData? priceData;
                try
                {
                    priceData = await provider.GetPriceAsync(tcgId, ct);
                }
                catch (ProviderException ex)
                {
                    var msg = $"Provider '{ex.ProviderName}' a échoué sur carte '{tcgId}' : {ex.Message}";
                    _logger.LogWarning(ex, "{Message}", msg);
                    errors.Add(msg);
                    cardsFailed++;
                    continue;
                }

                if (priceData is not null)
                    toUpsert.Add(MapToMarketPrice(cardId, priceData));
            }

            if (toUpsert.Count > 0)
            {
                await _priceRepo.UpsertBatchAsync(toUpsert, ct);
                await _uow.SaveChangesAsync(ct);
                pricesUpserted += toUpsert.Count;
            }
        }

        return new SyncPricesResult(pricesUpserted, cardsFailed, errors);
    }

    // ---- Helpers ----

    private ICardMetadataProvider? SelectMetadataProvider()
    {
        var list = _metadataProviders.ToList();
        return list.FirstOrDefault(p => p.Name == "pokemontcg") ?? list.FirstOrDefault();
    }

    private static CardMarketPrice MapToMarketPrice(Guid cardId, CardPriceData data)
        => CardMarketPrice.Create(
            cardId,
            source:      data.Source,
            variant:     null,
            marketPrice: data.RawMarketPrice,
            lowPrice:    null,
            highPrice:   null,
            currency:    data.Currency,
            psa7:        data.Psa7,
            psa8:        data.Psa8,
            psa9:        data.Psa9,
            psa95:       null,   // PriceCharting ne retourne pas de 9.5 distinct
            psa10:       data.Psa10);
}

public sealed record SyncCatalogResult(
    int SeriesUpserted,
    int SetsUpserted,
    int CardsUpserted,
    int PricesUpserted,
    IReadOnlyList<string> Errors);

public sealed record SyncPricesResult(
    int PricesUpserted,
    int CardsFailed,
    IReadOnlyList<string> Errors);
