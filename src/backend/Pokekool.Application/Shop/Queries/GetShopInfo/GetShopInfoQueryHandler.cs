using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopInfo;

internal sealed class GetShopInfoQueryHandler : IRequestHandler<GetShopInfoQuery, ShopDto>
{
    private readonly IShopRepository _shopRepo;

    public GetShopInfoQueryHandler(IShopRepository shopRepo) => _shopRepo = shopRepo;

    public async Task<ShopDto> Handle(GetShopInfoQuery request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);
        return ShopMapper.ToDto(shop);
    }
}
