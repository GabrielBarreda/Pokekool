using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopStock;

internal sealed class GetShopStockQueryHandler
    : IRequestHandler<GetShopStockQuery, PagedResult<ShopStockDto>>
{
    private readonly IShopStockRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetShopStockQueryHandler(
        IShopStockRepository repo, IPokemonCardRepository cardRepo,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _repo = repo;
        _cardRepo = cardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
    }

    public async Task<PagedResult<ShopStockDto>> Handle(GetShopStockQuery request, CancellationToken ct)
    {
        var type = ShopEnumMap.ParseProductTypeOrNull(request.Type);
        var (items, total) = await _repo.SearchAsync(type, request.Search, request.Page, request.PageSize, ct);

        // Résolution des cartes individuellement (déjà le comportement existant), mais
        // DisplayName est résolu en un seul lot après coup (ResolveManyAsync) plutôt que
        // carte par carte, pour éviter une requête de traduction par ligne de page.
        var cardsByStockId = new Dictionary<Guid, Domain.Entities.Pokemon.PokemonCard?>(items.Count);
        foreach (var s in items)
        {
            cardsByStockId[s.Id] = s.CardId.HasValue ? await _cardRepo.GetByIdAsync(s.CardId.Value, ct) : null;
        }

        var englishNamesByCardId = cardsByStockId.Values
            .Where(c => c is not null)
            .GroupBy(c => c!.Id)
            .ToDictionary(g => g.Key, g => g.First()!.Name);
        var displayNames = await _displayNameResolver.ResolveManyAsync(
            englishNamesByCardId, _displayLanguage.Language, ct);

        var dtos = new List<ShopStockDto>(items.Count);
        foreach (var s in items)
        {
            var card = cardsByStockId[s.Id];
            var displayName = card is not null && displayNames.TryGetValue(card.Id, out var dn) ? dn : null;
            dtos.Add(ShopMapper.ToDto(s, card, displayName));
        }

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PagedResult<ShopStockDto>(dtos, request.Page, request.PageSize, total, totalPages);
    }
}
