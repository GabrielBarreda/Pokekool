using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Commands.SyncPokemonTcg;

/// <param name="ForceRefresh">Re-synchronise même les sets déjà importés.</param>
/// <param name="SetTcgId">Si fourni, ne sync que ce set (forceRefresh implicite).</param>
public sealed record SyncPokemonTcgCommand(
    bool ForceRefresh = false,
    string? SetTcgId = null) : IRequest<SyncResultDto>;

public sealed record SyncResultDto(
    int SeriesImported,
    int SetsImported,
    int CardsImported,
    int CardsUpdated,
    int PricesUpserted,
    IReadOnlyList<string>? Errors = null);
