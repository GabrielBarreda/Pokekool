using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Pokemon.Commands.SyncCardPopulation;

public sealed class SyncCardPopulationCommandHandler
    : IRequestHandler<SyncCardPopulationCommand, SyncPopulationResultDto>
{
    private readonly IEnumerable<IPopulationProvider> _providers;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IPokemonSetRepository _setRepo;
    private readonly ICardPopulationRepository _populationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SyncCardPopulationCommandHandler> _logger;

    public SyncCardPopulationCommandHandler(
        IEnumerable<IPopulationProvider> providers,
        IPokemonCardRepository cardRepo,
        IPokemonSetRepository setRepo,
        ICardPopulationRepository populationRepo,
        IUnitOfWork uow,
        ILogger<SyncCardPopulationCommandHandler> logger)
    {
        _providers      = providers;
        _cardRepo       = cardRepo;
        _setRepo        = setRepo;
        _populationRepo = populationRepo;
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

        IReadOnlyList<PokemonCard> cards;
        if (request.CardIds is null || !request.CardIds.Any())
        {
            cards = await _cardRepo.GetAllAsync(cancellationToken);
        }
        else
        {
            cards = (await Task.WhenAll(
                request.CardIds.Select(id => _cardRepo.GetByIdAsync(id, cancellationToken))))
                .Where(c => c is not null)
                .Cast<PokemonCard>()
                .ToList();
        }

        var errors = new List<string>();
        int upserted = 0, failed = 0;

        foreach (var card in cards)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Résoudre le nom du set
            var set = await _setRepo.GetByIdAsync(card.SetId, cancellationToken);
            if (set is null)
            {
                errors.Add($"Set introuvable pour la carte '{card.TcgId}'.");
                failed++;
                continue;
            }

            foreach (var provider in providerList)
            {
                CardPopulationData? data;
                try
                {
                    data = await provider.GetPopulationAsync(card.Name, set.Name, cancellationToken);
                }
                catch (Exception ex)
                {
                    var msg = $"Provider '{provider.Name}' a échoué sur '{card.TcgId}' : {ex.Message}";
                    _logger.LogWarning(ex, "{Message}", msg);
                    errors.Add(msg);
                    failed++;
                    continue;
                }

                if (data is null) continue;

                var entity = CardPopulation.Create(
                    card.Id,
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
            }
        }

        _logger.LogInformation(
            "SyncCardPopulation terminé. Upserted={U} Failed={F} Errors={E}",
            upserted, failed, errors.Count);

        return new SyncPopulationResultDto(
            PopulationsUpserted: upserted,
            CardsFailed:         failed,
            Errors:              errors.Count > 0 ? errors : null);
    }
}
