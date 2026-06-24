using MediatR;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCards;

public sealed record GetCardsQuery(
    string? Search = null,
    Guid? SetId = null,
    string? Rarity = null,
    string? Supertype = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<PokemonCardSummaryDto>>;
