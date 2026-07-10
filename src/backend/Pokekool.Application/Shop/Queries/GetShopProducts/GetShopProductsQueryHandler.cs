using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopProducts;

internal sealed class GetShopProductsQueryHandler
    : IRequestHandler<GetShopProductsQuery, PagedResult<ShopProductDto>>
{
    private readonly IShopProductRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;

    public GetShopProductsQueryHandler(IShopProductRepository repo, IPokemonCardRepository cardRepo)
    {
        _repo = repo;
        _cardRepo = cardRepo;
    }

    public async Task<PagedResult<ShopProductDto>> Handle(GetShopProductsQuery request, CancellationToken ct)
    {
        var type = ShopEnumMap.ParseProductTypeOrNull(request.Type);

        var (items, total) = await _repo.SearchAsync(
            type, request.IsAvailable, request.Search, request.Page, request.PageSize, ct);

        var dtos = new List<ShopProductDto>(items.Count);
        foreach (var p in items)
        {
            var card = p.CardId.HasValue ? await _cardRepo.GetByIdAsync(p.CardId.Value, ct) : null;
            dtos.Add(ShopMapper.ToDto(p, card));
        }

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PagedResult<ShopProductDto>(dtos, request.Page, request.PageSize, total, totalPages);
    }
}
