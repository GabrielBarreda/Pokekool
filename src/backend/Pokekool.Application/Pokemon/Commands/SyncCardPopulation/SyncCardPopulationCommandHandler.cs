using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Pokemon.Commands.SyncCardPopulation;

public sealed class SyncCardPopulationCommandHandler
    : IRequestHandler<SyncCardPopulationCommand, SyncPopulationResultDto>
{
    private readonly IEnumerable<IPopulationProvider> _providers;
    private readonly IPokemonCardRepository           _cardRepo;
    private readonly IPokemonSetRepository            _setRepo;
    private readonly ICardPopulationRepository        _populationRepo;
    private readonly ISyncProgressReporter            _progress;
    private readonly IUnitOfWork                      _uow;
    private readonly ILogger<SyncCardPopulationCommandHandler> _logger;

    public SyncCardPopulationCommandHandler(
        IEnumerable<IPopulationProvider>           providers,
        IPokemonCardRepository                     cardRepo,
        IPokemonSetRepository                      setRepo,
        ICardPopulationRepository                  populationRepo,
        ISyncProgressReporter                      progress,
        IUnitOfWork                                uow,
        ILogger<SyncCardPopulationCommandHandler>  logger)
    {
        _providers      = providers;
        _cardRepo       = cardRepo;
        _setRepo        = setRepo;
        _populationRepo = populationRepo;
        _progress       = progress;
        _uow            = uow;
        _logger         = logger;
    }

    public async Task<SyncPopulationResultDto> Handle(
        SyncCardPopulationCommand request, CancellationToken cancellationToken)
    {
        var providerList = _providers.ToList();
        if (providerList.Count == 0)
        {
            _logger.LogWarning("SyncCardPopulation — aucun IPopulationProvider enregistré.");
            return new SyncPopulationResultDto(0, 0, ["Aucun IPopulationProvider enregistré."]);
        }

        // ── 1. Charger les cartes ─────────────────────────────────────────────
        IReadOnlyList<PokemonCard> cards;
        if (request.CardIds is null || !request.CardIds.Any())
        {
            cards = await _cardRepo.GetAllAsync(cancellationToken);
        }
        else
        {
            // Séquentiel : DbContext non thread-safe, pas de Task.WhenAll ici
            var cardList = new List<PokemonCard>();
            foreach (var id in request.CardIds)
            {
                var card = await _cardRepo.GetByIdAsync(id, cancellationToken);
                if (card is not null) cardList.Add(card);
            }
            cards = cardList;
        }

        // ── 2. Pré-charger tous les sets (accès DB séquentiel, évite la concurrence) ──
        var setIds  = cards.Select(c => c.SetId).Distinct();
        var sets    = new Dictionary<Guid, PokemonSet?>();
        foreach (var setId in setIds)
            sets[setId] = await _setRepo.GetByIdAsync(setId, cancellationToken);

        var cardById = cards.ToDictionary(c => c.Id);

        // ── 3. Scrape parallèle — 3 cartes simultanées max ───────────────────
        var semaphore = new SemaphoreSlim(3, 3);
        var collected = new ConcurrentBag<(Guid CardId, CardPopulationData Data)>();
        var errors    = new ConcurrentBag<string>();
        int failed    = 0;

        var tasks = cards.Select(async card =>
        {
            if (!sets.TryGetValue(card.SetId, out var set) || set is null)
            {
                errors.Add($"Set introuvable pour la carte '{card.TcgId}'.");
                Interlocked.Increment(ref failed);
                return;
            }

            var popRequest = new CardPopulationRequest(
                CardId:     card.Id,
                CardName:   card.Name,
                CardNumber: card.Number,
                SetId:      card.SetId,
                SetName:    set.Name);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                foreach (var provider in providerList)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    IReadOnlyList<CardPopulationData>? dataList;
                    try
                    {
                        dataList = await provider.GetPopulationAsync(popRequest, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Provider '{provider.Name}' a échoué sur '{card.TcgId}' : {ex.Message}";
                        _logger.LogWarning(ex, "{Message}", msg);
                        errors.Add(msg);
                        Interlocked.Increment(ref failed);
                        continue;
                    }

                    if (dataList is null) continue;

                    foreach (var data in dataList)
                        collected.Add((card.Id, data));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // ── 4. Upsert séquentiel en DB ────────────────────────────────────────
        int upserted = 0;
        int totalCards = cards.Count;
        bool firstEvent = true;

        foreach (var (cardId, data) in collected)
        {
            // Ignorer les entrées sans aucune donnée utile (bot-block, scraping raté)
            if (data.Total is null && data.Pop10 is null) continue;

            var entity = CardPopulation.Create(
                cardId,
                data.Source,
                data.Total,
                data.Pop10,
                data.Pop9,
                data.Pop8,
                data.Pop7,
                data.GemRate10);

            await _populationRepo.UpsertAsync(entity, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            upserted++;

            // Émettre un événement SSE
            cardById.TryGetValue(cardId, out var card);
            sets.TryGetValue(card?.SetId ?? Guid.Empty, out var cardSet);
            var grades = data.Source.Contains("bgs", StringComparison.OrdinalIgnoreCase)
                ? BuildBgsGradeLabels(data)
                : BuildPsaGradeLabels(data);

            _progress.Report(new SyncProgressEvent(
                SetName:    cardSet?.Name ?? "?",
                CardName:   card?.Name ?? "?",
                CardNumber: card?.Number ?? "?",
                Grades:     grades,
                Success:    true,
                Timestamp:  DateTime.UtcNow,
                SyncType:   "population",
                TotalItems: firstEvent ? totalCards : null));

            firstEvent = false;
        }

        _logger.LogInformation(
            "SyncCardPopulation terminé. Upserted={U} Failed={F} Errors={E}",
            upserted, failed, errors.Count);

        return new SyncPopulationResultDto(
            PopulationsUpserted: upserted,
            CardsFailed:         failed,
            Errors:              errors.IsEmpty ? null : errors.ToList());
    }

    private static string[] BuildPsaGradeLabels(CardPopulationData d)
    {
        var labels = new List<string>();
        if (d.Pop10 > 0) labels.Add("PSA10");
        if (d.Pop9  > 0) labels.Add("PSA9");
        if (d.Pop8  > 0) labels.Add("PSA8");
        if (d.Pop7  > 0) labels.Add("PSA7");
        return labels.ToArray();
    }

    private static string[] BuildBgsGradeLabels(CardPopulationData d)
    {
        var labels = new List<string>();
        if (d.Pop10 > 0) labels.Add("BGS10");
        if (d.Pop9  > 0) labels.Add("BGS9");
        if (d.Pop8  > 0) labels.Add("BGS8");
        if (d.Pop7  > 0) labels.Add("BGS7");
        return labels.ToArray();
    }
}
