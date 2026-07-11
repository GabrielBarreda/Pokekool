using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopPlvModel;

/// <summary>
/// Récupère l'URL du modèle 3D de la PLV (présentoir) de la boutique singleton.
/// Endpoint léger en lecture pour le viewer (shop:read).
/// </summary>
public sealed record GetShopPlvModelQuery : IRequest<ShopPlvModelDto>;

internal sealed class GetShopPlvModelQueryHandler : IRequestHandler<GetShopPlvModelQuery, ShopPlvModelDto>
{
    private readonly IShopRepository _shopRepo;

    public GetShopPlvModelQueryHandler(IShopRepository shopRepo) => _shopRepo = shopRepo;

    public async Task<ShopPlvModelDto> Handle(GetShopPlvModelQuery request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);
        return new ShopPlvModelDto(shop.PlvModel3dUrl);
    }
}
