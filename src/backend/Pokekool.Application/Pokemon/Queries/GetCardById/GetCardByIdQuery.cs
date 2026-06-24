using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCardById;

public sealed record GetCardByIdQuery(Guid Id) : IRequest<PokemonCardDto>;
