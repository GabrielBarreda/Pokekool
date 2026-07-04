using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.DataCollection;

/// <summary>
/// Orchestre la collecte de données via les providers enregistrés.
/// L'application appelle l'Aggregator — jamais les providers directement.
/// </summary>
public sealed class DataAggregator
{
    private readonly IEnumerable<ICardMetadataProvider>  _metadataProviders;
    private readonly IEnumerable<ICardPriceProvider>     _priceProviders;
    private readonly IPokemonSeriesRepository            _seriesRepo;
    private readonly IPokemonSetRepository               _setRepo;
    private readonly IPokemonCardRepository              _cardRepo;
    private readonly ICardMarketPriceRepository          _priceRepo;
    private readonly ICardGradedPriceRepository          _gradedPriceRepo;
    private readonly IUnitOfWork                         _uow;
    private readonly ILogger<DataAggregator>             _logger;
    private readonly ISyncProgressReporter               _progress;
    private readonly IServiceScopeFactory                _scopeFactory;

    public DataAggregator(
        IEnumerable<ICardMetadataProvider>  metadataProviders,
        IEnumerable<ICardPriceProvider>     priceProviders,
        IPokemonSeriesRepository            seriesRepo,
        IPokemonSetRepository               setRepo,
        IPokemonCardRepository              cardRepo,
        ICardMarketPriceRepository          priceRepo,
        ICardGradedPriceRepository          gradedPriceRepo,
        IUnitOfWork                         uow,
        ILogger<DataAggregator>             logger,
        ISyncProgressReporter               progress,
        IServiceScopeFactory                scopeFactory)
    {
        _metadataProviders = metadataProviders;
        _priceProviders    = priceProviders;
        _seriesRepo        = seriesRepo;
        _setRepo           = setRepo;
        _cardRepo          = cardRepo;
        _priceRepo         = priceRepo;
        _gradedPriceRepo   = gradedPriceRepo;
        _uow               = uow;
        _logger            = logger;
        _progress          = progress;
        _scopeFactory      = scopeFactory;
    }

    /// <summary>
    /// Synchronise le catalogue complet (séries, sets, cartes, prix embarqués).
    /// Utilise le premier ICardMetadataProvider disponible (priorité : "pokemontcg").
    /// Idempotent : relancer ne crée pas de doublons.
    /// </summary>
    public async Task<SyncCatalogResult> SyncCatalogAsync(
        bool forceRefresh = false,
        string? setTcgId  = null,
        CancellationToken ct = default)
    {
        var provider = SelectMetadataProvider();
        if (provider is null)
            return new SyncCatalogResult(0, 0, 0, 0, ["Aucun ICardMetadataProvider enregistré."]);

        if (setTcgId is not null) forceRefresh = true;

        _logger.LogInformation(
            "SyncCatalog démarré via le provider '{Provider}'. ForceRefresh={ForceRefresh} SetTcgId={SetTcgId}",
            provider.Name, forceRefresh, setTcgId ?? "all");

        var errors = new List<string>();
        int seriesUpserted = 0, setsUpserted = 0, cardsUpserted = 0, pricesUpserted = 0;

        var existingSeriesAll = await _seriesRepo.ListAsync(ct);
        var seriesByName      = existingSeriesAll
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

        if (setTcgId is not null)
        {
            sets = sets.Where(s => s.ProviderSetId == setTcgId).ToList();
            if (sets.Count == 0)
                return new SyncCatalogResult(0, 0, 0, 0, [$"Set TCG introuvable : {setTcgId}"]);
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

            await _uow.SaveChangesAsync(ct);

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

            var newCards         = new List<PokemonCard>();
            var pricesToSave     = new List<CardMarketPrice>();
            var gradedToSave     = new List<CardGradedPrice>();

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
                {
                    pricesToSave.Add(MapToMarketPrice(cardEntity.Id, priceData));
                    gradedToSave.AddRange(MapToGradedPrices(cardEntity.Id, priceData));
                }
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

            if (gradedToSave.Count > 0)
            {
                await _gradedPriceRepo.UpsertBatchAsync(gradedToSave, ct);
                await _uow.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Set {SetId} : {Cards} cartes, {Prices} prix bruts, {Graded} prix gradés.",
                setData.ProviderSetId, cards.Count, pricesToSave.Count, gradedToSave.Count);
        }

        _logger.LogInformation(
            "SyncCatalog terminé. Séries={S} Sets={Se} Cartes={C} Prix={P} Erreurs={E}",
            seriesUpserted, setsUpserted, cardsUpserted, pricesUpserted, errors.Count);

        return new SyncCatalogResult(seriesUpserted, setsUpserted, cardsUpserted, pricesUpserted, errors);
    }

    /// <summary>
    /// Met à jour les prix des cartes indiquées en interrogeant tous les ICardPriceProvider enregistrés.
    /// Traitement parallèle contrôlé (MaxParallelism=3) + écriture en BDD par batch de BatchSize cartes.
    /// </summary>
    public async Task<SyncPricesResult> SyncPricesAsync(
        IEnumerable<Guid> cardIds, CancellationToken ct = default)
    {
        var ids    = cardIds.ToList();
        var errors = new List<string>();
        int pricesUpserted = 0;
        int cardsFailed    = 0;

        var providers = _priceProviders.ToList();
        if (providers.Count == 0)
        {
            _logger.LogWarning("SyncPricesAsync appelé mais aucun ICardPriceProvider enregistré.");
            return new SyncPricesResult(0, 0, ["Aucun ICardPriceProvider enregistré."],
                new Dictionary<string, int>(), 0, 0);
        }

        // ── Pre-load : TcgId + infos carte (Set inclus) ─────────────────────
        var cardInfoById  = new Dictionary<Guid, PokemonCard>();
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
            cardInfoById[cardId]  = card;
            cardIdToTcgId[cardId] = card.TcgId;
        }

        // ── Sync par provider — tous en parallèle, DB sérialisée ────────────
        // Chaque provider tourne sur son propre thread (un site = un thread).
        // La concurrence au sein d'un provider reste limitée à MaxParallelism.
        // Chaque provider a son propre scope IServiceScope pour les écritures —
        // DbContext isolé par provider, pas de conflit EF Core entre providers parallèles.
        var providerResults = new ConcurrentBag<(int written, int failed, List<string> errs, Dictionary<string, int> gradedByCompany)>();

        var providerTasks = providers.Select(provider => RunProviderAsync(
            provider, cardIdToTcgId, cardInfoById,
            providerResults, ct)).ToList();

        await Task.WhenAll(providerTasks);

        var gradedByCompany = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (written, failed, errs, graded) in providerResults)
        {
            pricesUpserted += written;
            cardsFailed    += failed;
            errors.AddRange(errs);
            foreach (var (company, count) in graded)
            {
                gradedByCompany.TryGetValue(company, out var existing);
                gradedByCompany[company] = existing + count;
            }
        }

        return new SyncPricesResult(pricesUpserted, cardsFailed, errors, gradedByCompany, 0, 0);
    }

    // ── RunProviderAsync ──────────────────────────────────────────────────────

    /// <summary>
    /// Exécute un provider sur toutes les cartes avec MaxParallelism concurrence interne.
    /// Les écritures BDD se font dans un IServiceScope isolé par provider (DbContext dédié).
    /// </summary>
    private async Task RunProviderAsync(
        ICardPriceProvider                                                                        provider,
        Dictionary<Guid, string>                                                                  cardIdToTcgId,
        Dictionary<Guid, PokemonCard>                                                             cardInfoById,
        ConcurrentBag<(int written, int failed, List<string> errs, Dictionary<string, int> gradedByCompany)> results,
        CancellationToken                                                                         ct)
    {
        const int BatchSize      = 100;
        const int MaxParallelism = 3;

        var cardList          = cardIdToTcgId.ToList();
        int totalWritten      = 0;
        int totalFailed       = 0;
        var errs              = new List<string>();
        var gradedByCompany   = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        using var semaphore = new SemaphoreSlim(MaxParallelism, MaxParallelism);

        for (int offset = 0; offset < cardList.Count; offset += BatchSize)
        {
            if (ct.IsCancellationRequested) break;

            var chunk     = cardList.Skip(offset).Take(BatchSize).ToList();
            var chunkBag  = new ConcurrentBag<CardMarketPrice>();
            var gradedBag = new ConcurrentBag<CardGradedPrice>();
            int chunkFail = 0;
            var chunkErrs = new ConcurrentBag<string>();

            var tasks = chunk.Select(async kvp =>
            {
                var (cardId, tcgId) = kvp;
                await semaphore.WaitAsync(ct);
                try
                {
                    CardPriceData? priceData;
                    try
                    {
                        priceData = await provider.GetPriceAsync(tcgId, ct);
                    }
                    catch (ProviderException ex)
                    {
                        var msg = $"Provider '{ex.ProviderName}' — carte '{tcgId}' : {ex.Message}";
                        _logger.LogWarning(ex, "{Message}", msg);
                        chunkErrs.Add(msg);
                        Interlocked.Increment(ref chunkFail);

                        if (cardInfoById.TryGetValue(cardId, out var failedCard))
                            _progress.Report(new SyncProgressEvent(
                                failedCard.Set?.Name ?? "?",
                                failedCard.Name,
                                failedCard.Number,
                                [],
                                Success: false,
                                DateTime.UtcNow));
                        return;
                    }

                    if (priceData is not null)
                    {
                        chunkBag.Add(MapToMarketPrice(cardId, priceData));
                        foreach (var g in MapToGradedPrices(cardId, priceData))
                            gradedBag.Add(g);

                        if (cardInfoById.TryGetValue(cardId, out var successCard))
                            _progress.Report(new SyncProgressEvent(
                                successCard.Set?.Name ?? "?",
                                successCard.Name,
                                successCard.Number,
                                BuildGradeLabels(priceData),
                                Success: true,
                                DateTime.UtcNow));
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            // ── Flush BDD (sérialisé entre providers) ─────────────────────────
            totalFailed += chunkFail;
            foreach (var e in chunkErrs) errs.Add(e);

            var toWrite     = chunkBag.ToList();
            var gradedWrite = gradedBag.ToList();

            if (toWrite.Count > 0 || gradedWrite.Count > 0)
            {
                // Chaque provider a son propre scope d'écriture → son propre DbContext.
                // Plus de conflit EF Core entre providers parallèles.
                using var writeScope = _scopeFactory.CreateScope();
                var priceRepo   = writeScope.ServiceProvider.GetRequiredService<ICardMarketPriceRepository>();
                var gradedRepo  = writeScope.ServiceProvider.GetRequiredService<ICardGradedPriceRepository>();
                var uow         = writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                if (toWrite.Count > 0)
                {
                    await priceRepo.UpsertBatchAsync(toWrite, ct);
                    await uow.SaveChangesAsync(ct);
                    totalWritten += toWrite.Count;
                }
                if (gradedWrite.Count > 0)
                {
                    await gradedRepo.UpsertBatchAsync(gradedWrite, ct);
                    await uow.SaveChangesAsync(ct);

                    // Compter les grades écrits par société (PSA, CGC, BGS…)
                    foreach (var g in gradedWrite)
                    {
                        if (string.IsNullOrWhiteSpace(g.GradingCompany)) continue;
                        gradedByCompany.TryGetValue(g.GradingCompany, out var c);
                        gradedByCompany[g.GradingCompany] = c + 1;
                    }
                }
            }

            int done = Math.Min(offset + BatchSize, cardList.Count);
            if (done % 500 == 0 || done == cardList.Count)
                _logger.LogInformation(
                    "SyncPricesAsync [{Provider}] — {Done}/{Total} ({Percent}%), {Written} prix.",
                    provider.Name, done, cardList.Count,
                    (int)(done * 100.0 / cardList.Count), totalWritten);
            else
                _logger.LogDebug(
                    "SyncPricesAsync [{Provider}] — batch {Batch} : {Count} prix (total {Written}).",
                    provider.Name, (offset / BatchSize) + 1, toWrite.Count, totalWritten);
        }

        _logger.LogInformation(
            "SyncPricesAsync [{Provider}] terminé : {Upserted} prix, {Failed} erreurs.",
            provider.Name, totalWritten, totalFailed);

        results.Add((totalWritten, totalFailed, errs, gradedByCompany));
    }

    // ---- Helpers ----

    private ICardMetadataProvider? SelectMetadataProvider()
    {
        var list = _metadataProviders.ToList();
        return list.FirstOrDefault(p => p.Name == "pokemontcg") ?? list.FirstOrDefault();
    }

    private static string[] BuildGradeLabels(CardPriceData data)
    {
        if (data.GradedPrices is null or { Count: 0 }) return [];
        return data.GradedPrices
            .Select(g => $"{g.GradingCompany}{g.GradeValue:0.#}")
            .ToArray();
    }

    private static CardMarketPrice MapToMarketPrice(Guid cardId, CardPriceData data)
        => CardMarketPrice.Create(
            cardId,
            source:      data.Source,
            variant:     null,
            marketPrice: data.RawMarketPrice,
            lowPrice:    null,
            highPrice:   null,
            currency:    data.Currency);

    private static List<CardGradedPrice> MapToGradedPrices(Guid cardId, CardPriceData data)
    {
        if (data.GradedPrices is null or { Count: 0 }) return [];

        return data.GradedPrices
            .Select(g => CardGradedPrice.Create(
                cardId,
                data.Source,
                g.GradingCompany,
                g.GradeValue,
                g.Price,
                data.Currency))
            .ToList();
    }
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
    IReadOnlyList<string> Errors,
    IReadOnlyDictionary<string, int> GradedByCompany,
    int CardsWithoutAnyPrice,
    int CardsWithKeyGradesMissing);
