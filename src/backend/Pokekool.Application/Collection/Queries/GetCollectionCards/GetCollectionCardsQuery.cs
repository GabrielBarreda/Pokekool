using MediatR;
using Pokekool.Shared.Dtos.Collection;
using Pokekool.Shared.Dtos.Common;

namespace Pokekool.Application.Collection.Queries.GetCollectionCards;

public sealed record GetCollectionCardsQuery(
    Guid? SetId = null,
    string? Search = null,
    string? Rarity = null,
    int? Year = null,
    bool? OwnedOnly = null,
    bool? FavoritesOnly = null,
    string? SortBy = null,
    int Page = 1,
    int PageSize = 60
) : IRequest<PagedResult<CollectionCardDto>>;
