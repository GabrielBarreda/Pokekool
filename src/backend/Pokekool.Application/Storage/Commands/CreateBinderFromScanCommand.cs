using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Depuis un scan de classeur validé par l'employé :
///  - Pour chaque CardId (PokemonCard.Id) validé, crée un ShopStock (CardRaw, qté 1).
///  - Crée les StorageLocation liant ce ShopStock au classeur cible.
/// Réutilise le pipeline de scan existant (ICardVisionAnalyzer via ScanCollectionCommand).
/// </summary>
public sealed record CreateBinderFromScanCommand(
    Guid BinderId,
    IReadOnlyList<Guid> ValidatedCardIds) : IRequest<IReadOnlyList<StorageLocationDto>>;

internal sealed class CreateBinderFromScanCommandHandler
    : IRequestHandler<CreateBinderFromScanCommand, IReadOnlyList<StorageLocationDto>>
{
    private readonly IStorageBinderRepository _binderRepo;
    private readonly IStorageLocationRepository _locationRepo;
    private readonly IShopStockRepository _stockRepo;
    private readonly IShopRepository _shopRepo;
    private readonly IUnitOfWork _uow;

    public CreateBinderFromScanCommandHandler(
        IStorageBinderRepository binderRepo,
        IStorageLocationRepository locationRepo,
        IShopStockRepository stockRepo,
        IShopRepository shopRepo,
        IUnitOfWork uow)
    {
        _binderRepo = binderRepo;
        _locationRepo = locationRepo;
        _stockRepo = stockRepo;
        _shopRepo = shopRepo;
        _uow = uow;
    }

    public async Task<IReadOnlyList<StorageLocationDto>> Handle(
        CreateBinderFromScanCommand request, CancellationToken ct)
    {
        _ = await _binderRepo.GetByIdAsync(request.BinderId, ct)
            ?? throw new DomainException("storage.binder.not_found", "Classeur introuvable.");

        var shop = await _shopRepo.GetOrCreateAsync(ct);
        var results = new List<StorageLocationDto>(request.ValidatedCardIds.Count);

        foreach (var cardId in request.ValidatedCardIds)
        {
            var stock = ShopStock.Create(
                shopId: shop.Id,
                cardId: cardId,
                productName: null,
                type: ShopProductType.CardRaw,
                language: "FR",
                condition: null,
                gradingCompany: null,
                gradeValue: null,
                quantityTotal: 1,
                quantityAvailable: 1,
                quantityReserved: 0,
                purchasePrice: null,
                purchaseCurrency: null,
                notes: "Créé via scan classeur");

            await _stockRepo.AddAsync(stock, ct);

            var location = StorageLocation.Create(request.BinderId, "shop_stock", stock.Id, null);
            await _locationRepo.AddAsync(location, ct);
            results.Add(StorageMapper.ToDto(location));
        }

        await _uow.SaveChangesAsync(ct);
        return results;
    }
}
