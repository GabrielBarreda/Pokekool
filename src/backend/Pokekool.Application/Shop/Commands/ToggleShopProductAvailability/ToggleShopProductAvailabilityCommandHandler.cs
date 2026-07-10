using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.ToggleShopProductAvailability;

internal sealed class ToggleShopProductAvailabilityCommandHandler
    : IRequestHandler<ToggleShopProductAvailabilityCommand, ShopProductDto>
{
    private readonly IShopProductRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IUnitOfWork _uow;

    public ToggleShopProductAvailabilityCommandHandler(
        IShopProductRepository repo, IPokemonCardRepository cardRepo, IUnitOfWork uow)
    {
        _repo = repo;
        _cardRepo = cardRepo;
        _uow = uow;
    }

    public async Task<ShopProductDto> Handle(ToggleShopProductAvailabilityCommand request, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.ProductNotFound(request.Id);

        product.SetAvailability(request.IsAvailable);
        await _uow.SaveChangesAsync(ct);

        var card = product.CardId.HasValue ? await _cardRepo.GetByIdAsync(product.CardId.Value, ct) : null;
        return ShopMapper.ToDto(product, card);
    }
}
