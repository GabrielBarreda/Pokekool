using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateShopProduct;

internal sealed class UpdateShopProductCommandHandler
    : IRequestHandler<UpdateShopProductCommand, ShopProductDto>
{
    private readonly IShopProductRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IUnitOfWork _uow;

    public UpdateShopProductCommandHandler(
        IShopProductRepository repo, IPokemonCardRepository cardRepo, IUnitOfWork uow)
    {
        _repo = repo;
        _cardRepo = cardRepo;
        _uow = uow;
    }

    public async Task<ShopProductDto> Handle(UpdateShopProductCommand request, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.ProductNotFound(request.Id);

        product.Update(
            ShopEnumMap.ParseProductType(request.Type),
            request.CardId, request.ProductName, request.Condition, request.GradingCompany,
            request.GradeValue, request.Price, request.Currency ?? "EUR", request.Quantity,
            request.IsAvailable, request.Description, request.PhotoUrl);

        await _uow.SaveChangesAsync(ct);

        var card = product.CardId.HasValue ? await _cardRepo.GetByIdAsync(product.CardId.Value, ct) : null;
        return ShopMapper.ToDto(product, card);
    }
}
