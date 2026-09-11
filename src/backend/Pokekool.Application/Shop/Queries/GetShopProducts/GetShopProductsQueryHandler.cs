using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopProducts;

internal sealed class GetShopProductsQueryHandler
    : IRequestHandler<GetShopProductsQuery, PagedResult<ShopProductDto>>
{
    private readonly IShopProductRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetShopProductsQueryHandler(
        IShopProductRepository repo, IPokemonCardRepository cardRepo,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _repo = repo;
        _cardRepo = cardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
    }

    public async Task<PagedResult<ShopProductDto>> Handle(GetShopProductsQuery request, CancellationToken ct)
    {
        var type = ShopEnumMap.ParseProductTypeOrNull(request.Type);

        var (items, total) = await _repo.SearchAsync(
            type, request.IsAvailable, request.Search, request.Page, request.PageSize, ct);

        // Résolution des cartes individuellement (déjà le comportement existant), mais
        // DisplayName est résolu en un seul lot après coup (ResolveManyAsync) plutôt que
        // carte par carte, pour éviter une requête de traduction par ligne de page.
        var cardsByProductId = new Dictionary<Guid, Domain.Entities.Pokemon.PokemonCard?>(items.Count);
        foreach (var p in items)
        {
            cardsByProductId[p.Id] = p.CardId.HasValue ? await _cardRepo.GetByIdAsync(p.CardId.Value, ct) : null;
        }

        var englishNamesByCardId = cardsByProductId.Values
            .Where(c => c is not null)
            .GroupBy(c => c!.Id)
            .ToDictionary(g => g.Key, g => g.First()!.Name);
        var displayNames = await _displayNameResolver.ResolveManyAsync(
            englishNamesByCardId, _displayLanguage.Language, ct);

        var dtos = new List<ShopProductDto>(items.Count);
        foreach (var p in items)
        {
            var card = cardsByProductId[p.Id];
            var displayName = card is not null && displayNames.TryGetValue(card.Id, out var dn) ? dn : null;
            dtos.Add(ShopMapper.ToDto(p, card, displayName));
        }

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PagedResult<ShopProductDto>(dtos, request.Page, request.PageSize, total, totalPages);
    }
}
