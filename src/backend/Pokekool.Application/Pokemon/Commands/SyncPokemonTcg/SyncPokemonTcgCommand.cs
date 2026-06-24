using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Commands.SyncPokemonTcg;

public sealed record SyncPokemonTcgCommand(bool ForceRefresh = false) : IRequest<SyncResultDto>;

public sealed record SyncResultDto(
    int SeriesImported,
    int SetsImported,
    int CardsImported,
    int CardsUpdated,
    int PricesUpserted,
    IReadOnlyList<string>? Errors = null);
