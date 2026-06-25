using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;
using Pokekool.Shared.Dtos.Common;

namespace Pokekool.Application.Collection.Queries.GetCollectionCards;

public sealed class GetCollectionCardsQueryHandler
    : IRequestHandler<GetCollectionCardsQuery, PagedResult<CollectionCardDto>>
{
    private readonly IUserCollectionRepository _collectionRepo;
    private readonly ICurrentUserService _currentUser;

    public GetCollectionCardsQueryHandler(
        IUserCollectionRepository collectionRepo,
        ICurrentUserService currentUser)
    {
        _collectionRepo = collectionRepo;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CollectionCardDto>> Handle(
        GetCollectionCardsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var (items, totalCount) = await _collectionRepo.GetCardsPagedAsync(
            userId,
            request.SetId,
            request.Search,
            request.Rarity,
            request.Year,
            request.OwnedOnly,
            request.FavoritesOnly,
            request.SortBy,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(p => new CollectionCardDto(
            p.Id.ToString(),
            p.TcgId,
            p.Number,
            p.Name,
            p.Supertype,
            p.Rarity,
            p.Types,
            p.ImageSmall,
            p.ImageLarge,
            p.SetId.ToString(),
            p.SetName,
            p.SetSymbolUrl,
            p.ReleaseYear,
            p.IsOwned,
            p.IsFavorite,
            p.IsPriority)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        return new PagedResult<CollectionCardDto>(dtos, request.Page, request.PageSize, totalCount, totalPages);
    }
}
