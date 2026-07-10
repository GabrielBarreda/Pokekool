using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopStock;

internal sealed class GetShopStockQueryHandler
    : IRequestHandler<GetShopStockQuery, PagedResult<ShopStockDto>>
{
    private readonly IShopStockRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;

    public GetShopStockQueryHandler(IShopStockRepository repo, IPokemonCardRepository cardRepo)
    {
        _repo = repo;
        _cardRepo = cardRepo;
    }

    public async Task<PagedResult<ShopStockDto>> Handle(GetShopStockQuery request, CancellationToken ct)
    {
        var type = ShopEnumMap.ParseProductTypeOrNull(request.Type);
        var (items, total) = await _repo.SearchAsync(type, request.Search, request.Page, request.PageSize, ct);

        var dtos = new List<ShopStockDto>(items.Count);
        foreach (var s in items)
        {
            var card = s.CardId.HasValue ? await _cardRepo.GetByIdAsync(s.CardId.Value, ct) : null;
            dtos.Add(ShopMapper.ToDto(s, card));
        }

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        return new PagedResult<ShopStockDto>(dtos, request.Page, request.PageSize, total, totalPages);
    }
}
