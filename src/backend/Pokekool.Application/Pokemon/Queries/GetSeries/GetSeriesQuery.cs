using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSeries;

public sealed record GetSeriesQuery : IRequest<IReadOnlyList<PokemonSeriesDto>>;
