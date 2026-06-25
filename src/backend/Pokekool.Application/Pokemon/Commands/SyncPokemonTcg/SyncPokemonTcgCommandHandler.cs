using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;

namespace Pokekool.Application.Pokemon.Commands.SyncPokemonTcg;

public sealed class SyncPokemonTcgCommandHandler : IRequestHandler<SyncPokemonTcgCommand, SyncResultDto>
{
    private readonly DataAggregator _aggregator;
    private readonly ILogger<SyncPokemonTcgCommandHandler> _logger;

    public SyncPokemonTcgCommandHandler(
        DataAggregator aggregator,
        ILogger<SyncPokemonTcgCommandHandler> logger)
    {
        _aggregator = aggregator;
        _logger     = logger;
    }

    public async Task<SyncResultDto> Handle(
        SyncPokemonTcgCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SyncPokemonTcgCommand reçu. ForceRefresh={ForceRefresh} SetTcgId={SetTcgId}",
            request.ForceRefresh, request.SetTcgId ?? "all");

        var result = await _aggregator.SyncCatalogAsync(
            request.ForceRefresh, request.SetTcgId, cancellationToken);

        return new SyncResultDto(
            SeriesImported: result.SeriesUpserted,
            SetsImported:   result.SetsUpserted,
            CardsImported:  result.CardsUpserted,
            CardsUpdated:   0,              // fusionné dans CardsUpserted
            PricesUpserted: result.PricesUpserted,
            Errors:         result.Errors.Count > 0 ? result.Errors : null);
    }
}
