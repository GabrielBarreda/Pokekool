using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;
using Pokekool.Shared.Dtos.Common;

namespace Pokekool.Application.Collection.Queries.GetCollectionCards;

public sealed class GetCollectionCardsQueryHandler
    : IRequestHandler<GetCollectionCardsQuery, PagedResult<CollectionCardDto>>
{
    private readonly IUserCollectionRepository _collectionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetCollectionCardsQueryHandler(
        IUserCollectionRepository collectionRepo,
        ICurrentUserService currentUser,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _collectionRepo = collectionRepo;
        _currentUser = currentUser;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
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

        var englishNamesByCardId = items.ToDictionary(p => p.Id, p => p.Name);
        var displayNames = await _displayNameResolver.ResolveManyAsync(
            englishNamesByCardId, _displayLanguage.Language, cancellationToken);

        var dtos = items.Select(p => new CollectionCardDto(
            p.Id.ToString(),
            p.TcgId,
            p.Number,
            p.Name,
            displayNames.TryGetValue(p.Id, out var displayName) ? displayName : p.Name,
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
