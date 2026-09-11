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
    private readonly ICardTranslationRepository          _translationRepo;
    private readonly ITcgDexClient                       _tcgDexClient;

    /// <summary>Langue par défaut synchronisée automatiquement à la fin d'un import de set —
    /// pas un littéral SQL, seulement la valeur par défaut passée en paramètre (voir ADR-004,
    /// mémoire Obsidian du projet).</summary>
    private const string AutoTranslationLanguage = "fr";

    /// <summary>
    /// Tentatives supplémentaires (en plus de la 1ère passe) sur les sets dont le fetch
    /// catalogue échoue — typiquement un rate limit provider (ex. pokemontcg.io sans clé API).
    /// 2 tentatives supplémentaires = 3 essais au total par set avant abandon définitif.
    /// </summary>
    private const int MaxCatalogRetryPasses = 2;

    /// <summary>Délai entre deux passes de retry — supérieur aux fenêtres de rate limit "par
    /// minute" habituelles, pour laisser le quota se reconstituer côté provider.</summary>
    private static readonly TimeSpan CatalogRetryDelay = TimeSpan.FromSeconds(65);

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
        IServiceScopeFactory                scopeFactory,
        ICardTranslationRepository          translationRepo,
        ITcgDexClient                       tcgDexClient)
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
        _translationRepo   = translationRepo;
        _tcgDexClient      = tcgDexClient;
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
        // Un événement SSE par set traversé (pas par carte — trop verbeux sur un catalogue
        // complet) pour que l'overlay de progression web affiche ce sync comme n'importe quel
        // autre (voir SyncProgressOverlayComponent, badge "📦 SYNC CATALOG" déjà géré côté UI).
        var firstProgressEvent = true;

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

        // ── Retry multi-passes sur échec provider (rate limit typiquement) ───────────
        // 1ère passe sur tous les sets ; les sets en échec catalogue sont réessayés lors des
        // passes suivantes (avec délai, pour laisser un éventuel rate limit se reconstituer),
        // jusqu'à MaxCatalogRetryPasses tentatives supplémentaires. Un set encore en échec après
        // la dernière passe est alors définitivement compté en erreur.
        var setsToProcess          = sets.ToList();
        var translationRetryQueue  = new List<(Guid SetId, string ProviderSetId, string SetName)>();

        for (var pass = 0; setsToProcess.Count > 0; pass++)
        {
            var isLastPass = pass >= MaxCatalogRetryPasses;

            if (pass > 0)
            {
                _logger.LogInformation(
                    "SyncCatalog — passe de retry {Pass}/{Max} sur {Count} set(s) en échec, attente {Delay}s.",
                    pass, MaxCatalogRetryPasses, setsToProcess.Count, CatalogRetryDelay.TotalSeconds);
                await Task.Delay(CatalogRetryDelay, ct);
            }

            var stillFailing = new List<CardSetData>();

        foreach (var setData in setsToProcess)
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

            // ---- Traductions (auto, best-effort) — AVANT le skip catalogue ----
            // Un set "déjà synchronisé" (skip catalogue ci-dessous) peut très bien avoir des
            // cartes en base sans traduction (ex. sets importés avant que ce pipeline existe).
            // Cet appel tourne pour CHAQUE set traversé, skip ou non : SyncSetTranslationsAsync
            // ne retraite que les cartes réellement sans traduction (voir son implémentation),
            // donc aucun surcoût pour un set déjà entièrement traduit.
            var translationsOk1 = await SyncSetTranslationsAsync(setEntity.Id, setData.ProviderSetId, AutoTranslationLanguage, ct);
            if (!translationsOk1)
                translationRetryQueue.Add((setEntity.Id, setData.ProviderSetId, setData.Name));

            if (!forceRefresh && existingSet is not null && existingSet.Cards.Count > 0)
            {
                _logger.LogDebug("Set {SetId} déjà synchronisé, skip.", setData.ProviderSetId);
                _progress.Report(new SyncProgressEvent(
                    SetName:    setData.Name,
                    CardName:   "déjà à jour",
                    CardNumber: "",
                    Grades:     [],
                    Success:    true,
                    Timestamp:  DateTime.UtcNow,
                    SyncType:   "catalog",
                    TotalItems: firstProgressEvent ? sets.Count : null));
                firstProgressEvent = false;
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

                if (!isLastPass)
                {
                    // Rate limit probable : on retente ce set lors de la prochaine passe plutôt
                    // que de le compter en erreur définitive tout de suite.
                    _logger.LogWarning(ex, "{Message} — sera retenté (passe {Pass}).", msg, pass + 1);
                    stillFailing.Add(setData);
                    continue;
                }

                _logger.LogWarning(ex, "{Message} — abandon après {Max} tentative(s) supplémentaire(s).", msg, MaxCatalogRetryPasses);
                errors.Add(msg);
                _progress.Report(new SyncProgressEvent(
                    SetName:    setData.Name,
                    CardName:   "échec provider",
                    CardNumber: "",
                    Grades:     [],
                    Success:    false,
                    Timestamp:  DateTime.UtcNow,
                    SyncType:   "catalog",
                    TotalItems: firstProgressEvent ? sets.Count : null));
                firstProgressEvent = false;
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

            // ---- Traductions (auto, best-effort) — APRÈS upsert des cartes de ce run ----
            // Couvre les cartes nouvellement insérées ET celles mises à jour (Update()) dans
            // cette itération — un appel précédent (avant le skip catalogue, plus haut) a déjà
            // couvert le cas "set skip, cartes déjà en base" ; celui-ci couvre le cas "set
            // traité ce run, certaines cartes existaient déjà mais sans traduction". Idempotent
            // (voir SyncSetTranslationsAsync), donc pas de risque à l'appeler systématiquement.
            // Ne bloque jamais le sync catalogue : TCGdex peut être injoignable (ex. politique
            // réseau côté hébergement) sans empêcher l'import des cartes elles-mêmes.
            var translationsOk2 = await SyncSetTranslationsAsync(setEntity.Id, setData.ProviderSetId, AutoTranslationLanguage, ct);
            if (!translationsOk2)
                translationRetryQueue.Add((setEntity.Id, setData.ProviderSetId, setData.Name));

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

            _progress.Report(new SyncProgressEvent(
                SetName:    setData.Name,
                CardName:   $"{cards.Count} carte(s)",
                CardNumber: "",
                Grades:     [],
                Success:    true,
                Timestamp:  DateTime.UtcNow,
                SyncType:   "catalog",
                TotalItems: firstProgressEvent ? sets.Count : null));
            firstProgressEvent = false;
        }

            setsToProcess = stillFailing;
        }

        // ── Retry final des traductions en échec (TCGdex indisponible/rate-limit pendant le run) ──
        // Une seule tentative supplémentaire suffit ici : contrairement au catalogue (bloquant
        // pour l'import des cartes), une traduction manquante n'empêche rien — elle sera de
        // toute façon retentée automatiquement lors du prochain SyncCatalog (voir l'appel
        // "AVANT le skip catalogue" plus haut, qui couvre les sets déjà en base).
        if (translationRetryQueue.Count > 0)
        {
            _logger.LogInformation(
                "SyncCatalog — retry final traductions sur {Count} set(s).", translationRetryQueue.Count);

            foreach (var (retrySetId, retryProviderSetId, retrySetName) in translationRetryQueue)
            {
                ct.ThrowIfCancellationRequested();
                var ok = await SyncSetTranslationsAsync(retrySetId, retryProviderSetId, AutoTranslationLanguage, ct);
                if (!ok)
                {
                    _logger.LogWarning(
                        "SyncCatalog — traduction toujours en échec pour le set {Set} après retry final.",
                        retrySetName);
                }
            }
        }

        _logger.LogInformation(
            "SyncCatalog terminé. Séries={S} Sets={Se} Cartes={C} Prix={P} Erreurs={E}",
            seriesUpserted, setsUpserted, cardsUpserted, pricesUpserted, errors.Count);

        return new SyncCatalogResult(seriesUpserted, setsUpserted, cardsUpserted, pricesUpserted, errors);
    }

    /// <summary>
    /// Traduit les cartes d'un set n'ayant pas encore de nom pour la langue donnée, via TCGdex.
    /// Best-effort : toute erreur (réseau, set introuvable côté TCGdex, etc.) est loggée et
    /// avalée — ne doit jamais faire échouer SyncCatalogAsync (voir appel ci-dessus).
    /// Retourne false en cas d'échec (pour permettre à l'appelant de retenter ce set plus tard —
    /// voir translationRetryQueue dans SyncCatalogAsync), true sinon (y compris no-op).
    /// </summary>
    private async Task<bool> SyncSetTranslationsAsync(
        Guid setId, string providerSetId, string language, CancellationToken ct)
    {
        try
        {
            var cardIdsToTranslate = await _translationRepo.GetCardIdsWithoutTranslationAsync(setId, language, ct);
            if (cardIdsToTranslate.Count == 0)
                return true;

            var cardsInSet = await _cardRepo.GetIdAndTcgIdBySetIdAsync(setId, ct);
            var candidateCards = cardsInSet.Where(c => cardIdsToTranslate.Contains(c.Id)).ToList();
            if (candidateCards.Count == 0)
                return true;

            var tcgDexNames = await _tcgDexClient.GetCardNamesBySetAsync(providerSetId, language, ct);
            if (tcgDexNames.Count == 0)
            {
                _logger.LogDebug(
                    "Set {SetId} : aucun nom '{Language}' retourné par TCGdex, skip traduction auto.",
                    providerSetId, language);
                return false;
            }

            var nameByTcgId = tcgDexNames
                .GroupBy(n => n.TcgId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

            var toUpsert = new List<CardTranslation>();
            foreach (var (cardId, tcgId) in candidateCards)
            {
                if (nameByTcgId.TryGetValue(tcgId, out var translatedName) &&
                    !string.IsNullOrWhiteSpace(translatedName))
                {
                    toUpsert.Add(CardTranslation.Create(cardId, language, translatedName));
                }
            }

            if (toUpsert.Count == 0)
                return false;

            await _translationRepo.UpsertRangeAsync(toUpsert, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Traductions '{Language}' auto-sync pour le set {SetId} : {Count} cartes.",
                language, providerSetId, toUpsert.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Échec traduction auto '{Language}' pour le set {SetId} — sync catalogue non impacté.",
                language, providerSetId);
            return false;
        }
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
