using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Pokemon.Commands.SyncCardPrices;

public sealed class SyncCardPricesCommandHandler
    : IRequestHandler<SyncCardPricesCommand, SyncPricesResultDto>
{
    private readonly DataAggregator _aggregator;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardMarketPriceRepository _priceRepo;
    private readonly ILogger<SyncCardPricesCommandHandler> _logger;

    // Source de référence pour le filtre de fraîcheur (provider principal)
    private const string StalenessSource = "pricecharting";

    public SyncCardPricesCommandHandler(
        DataAggregator aggregator,
        IPokemonCardRepository cardRepo,
        ICardMarketPriceRepository priceRepo,
        ILogger<SyncCardPricesCommandHandler> logger)
    {
        _aggregator = aggregator;
        _cardRepo   = cardRepo;
        _priceRepo  = priceRepo;
        _logger     = logger;
    }

    public async Task<SyncPricesResultDto> Handle(
        SyncCardPricesCommand request, CancellationToken cancellationToken)
    {
        IEnumerable<Guid> cardIds;

        if (request.CardIds is not null && request.CardIds.Any())
        {
            // IDs explicites fournis (sync ciblé page courante)
            cardIds = request.CardIds;
            _logger.LogInformation(
                "SyncCardPricesCommand — sync ciblé : {Count} carte(s).", request.CardIds.Count());
        }
        else if (request.SetId.HasValue)
        {
            // Sync de toutes les cartes d'un set (avec filtre de fraîcheur si applicable)
            if (request.StalenessHours == 0)
            {
                _logger.LogInformation(
                    "SyncCardPricesCommand — force-sync set {SetId}.", request.SetId);
                cardIds = await _cardRepo.GetIdsBySetIdAsync(request.SetId.Value, cancellationToken);
            }
            else
            {
                var stale = await _priceRepo.GetStaleCardIdsAsync(
                    StalenessSource, request.StalenessHours, request.SetId, cancellationToken);
                _logger.LogInformation(
                    "SyncCardPricesCommand — set {SetId}, fraîcheur {Hours}h : {Count} carte(s) périmée(s).",
                    request.SetId, request.StalenessHours, stale.Count);

                if (stale.Count == 0)
                {
                    _logger.LogInformation("Toutes les cartes du set sont à jour — rien à synchroniser.");
                    return new SyncPricesResultDto(0, 0, null);
                }
                cardIds = stale;
            }
        }
        else if (request.StalenessHours == 0)
        {
            // Force-sync global de toutes les cartes
            _logger.LogInformation("SyncCardPricesCommand — force-sync global (StalenessHours=0).");
            var all = await _cardRepo.GetAllAsync(cancellationToken);
            cardIds = all.Select(c => c.Id);
        }
        else
        {
            // Filtre de fraîcheur global
            var stale = await _priceRepo.GetStaleCardIdsAsync(
                StalenessSource, request.StalenessHours, null, cancellationToken);

            _logger.LogInformation(
                "SyncCardPricesCommand — filtre fraîcheur {Hours}h global : {Count} carte(s) périmée(s).",
                request.StalenessHours, stale.Count);

            if (stale.Count == 0)
            {
                _logger.LogInformation("Toutes les cartes sont à jour — rien à synchroniser.");
                return new SyncPricesResultDto(0, 0, null);
            }
            cardIds = stale;
        }

        var result = await _aggregator.SyncPricesAsync(cardIds, cancellationToken);

        return new SyncPricesResultDto(
            PricesUpserted: result.PricesUpserted,
            CardsFailed:    result.CardsFailed,
            Errors:         result.Errors.Count > 0 ? result.Errors : null);
    }
}
