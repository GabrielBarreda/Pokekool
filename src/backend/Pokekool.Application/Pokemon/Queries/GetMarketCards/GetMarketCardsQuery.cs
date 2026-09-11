using MediatR;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetMarketCards;

/// <param name="SetId">null = toutes les cartes du catalogue, tous sets confondus (mode "Tous").</param>
public sealed record GetMarketCardsQuery(
    Guid?   SetId,
    string? Search = null,
    string? Rarity = null,
    string? Supertype = null,
    int     Page = 1,
    int     PageSize = 60) : IRequest<PagedResult<MarketCardDto>>;
