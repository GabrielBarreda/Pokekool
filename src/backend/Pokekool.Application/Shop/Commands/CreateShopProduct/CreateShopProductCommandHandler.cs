using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
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
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;
    private readonly IUnitOfWork _uow;

    public CreateShopProductCommandHandler(
        IShopRepository shopRepo, IShopProductRepository repo,
        IPokemonCardRepository cardRepo,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage,
        IUnitOfWork uow)
    {
        _shopRepo = shopRepo;
        _repo = repo;
        _cardRepo = cardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
        _uow = uow;
    }

    public async Task<ShopProductDto> Handle(CreateShopProductCommand request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);

        var product = ShopProduct.Create(
            shop.Id,
            ShopEnumMap.ParseProductType(request.Type),
            request.CardId, request.ProductName,
            request.Language,
            request.Condition, request.GradingCompany,
            request.GradeValue, request.Price, request.Currency ?? "EUR", request.Quantity,
            request.IsAvailable, request.Description, request.PhotoUrl);

        await _repo.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        var card = product.CardId.HasValue ? await _cardRepo.GetByIdAsync(product.CardId.Value, ct) : null;
        var displayName = card is not null
            ? await _displayNameResolver.ResolveAsync(card.Id, card.Name, _displayLanguage.Language, ct)
            : null;
        return ShopMapper.ToDto(product, card, displayName);
    }
}
