using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateShopInfo;

internal sealed class UpdateShopInfoCommandHandler : IRequestHandler<UpdateShopInfoCommand, ShopDto>
{
    private readonly IShopRepository _shopRepo;
    private readonly IUnitOfWork _uow;

    public UpdateShopInfoCommandHandler(IShopRepository shopRepo, IUnitOfWork uow)
    {
        _shopRepo = shopRepo;
        _uow = uow;
    }

    public async Task<ShopDto> Handle(UpdateShopInfoCommand request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);

        shop.Update(
            request.Name, request.Description, request.Address, request.City,
            request.ZipCode, request.GoogleMapsUrl, request.HoursJson, request.Services,
            request.InstagramUrl, request.TiktokUrl, request.PhotoUrls);

        await _uow.SaveChangesAsync(ct);
        return ShopMapper.ToDto(shop);
    }
}
