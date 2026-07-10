using MediatR;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.CreateShopProduct;

internal sealed class CreateShopProductCommandHandler
    : IRequestHandler<CreateShopProductCommand, ShopProductDto>
{
    private readonly IShopRepository _shopRepo;
    private readonly IShopProductRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IUnitOfWork _uow;

    public CreateShopProductCommandHandler(
        IShopRepository shopRepo, IShopProductRepository repo,
        IPokemonCardRepository cardRepo, IUnitOfWork uow)
    {
        _shopRepo = shopRepo;
        _repo = repo;
        _cardRepo = cardRepo;
        _uow = uow;
    }

    public async Task<ShopProductDto> Handle(CreateShopProductCommand request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);

        var product = ShopProduct.Create(
            shop.Id,
            ShopEnumMap.ParseProductType(request.Type),
            request.CardId, request.ProductName, request.Condition, request.GradingCompany,
            request.GradeValue, request.Price, request.Currency ?? "EUR", request.Quantity,
            request.IsAvailable, request.Description, request.PhotoUrl);

        await _repo.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        var card = product.CardId.HasValue ? await _cardRepo.GetByIdAsync(product.CardId.Value, ct) : null;
        return ShopMapper.ToDto(product, card);
    }
}
