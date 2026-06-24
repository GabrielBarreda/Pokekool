using MediatR;

namespace Pokekool.Application.Pokemon.Commands.SyncCardPopulation;

/// <summary>
/// Synchronise les données de population PSA des cartes indiquées.
/// Si CardIds est null ou vide, toutes les cartes sont traitées.
/// </summary>
public sealed record SyncCardPopulationCommand(IEnumerable<Guid>? CardIds = null)
    : IRequest<SyncPopulationResultDto>;

public sealed record SyncPopulationResultDto(
    int PopulationsUpserted,
    int CardsFailed,
    IReadOnlyList<string>? Errors);
