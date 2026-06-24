using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSets;

public sealed record GetSetsQuery(Guid? SeriesId = null) : IRequest<IReadOnlyList<PokemonSetDto>>;
