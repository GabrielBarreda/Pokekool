using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSetById;

public sealed record GetSetByIdQuery(Guid Id) : IRequest<PokemonSetDto>;
