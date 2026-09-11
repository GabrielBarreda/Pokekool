using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.CreateShopStock;

internal sealed class CreateShopStockCommandHandler
    : IRequestHandler<CreateShopStockCommand, ShopStockDto>
{
    private readonly IShopRepository _shopRepo;
    private readonly IShopStockRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;
    private readonly IUnitOfWork _uow;

    public CreateShopStockCommandHandler(
        IShopRepository shopRepo, IShopStockRepository repo,
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

    public async Task<ShopStockDto> Handle(CreateShopStockCommand request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);

        var stock = ShopStock.Create(
            shop.Id, request.CardId, request.ProductName,
            ShopEnumMap.ParseProductType(request.Type),
            request.Language,
            request.Condition, request.GradingCompany, request.GradeValue,
            request.QuantityTotal, request.QuantityAvailable, request.QuantityReserved,
            request.PurchasePrice, request.PurchaseCurrency, request.Notes);

        await _repo.AddAsync(stock, ct);
        await _uow.SaveChangesAsync(ct);

        var card = stock.CardId.HasValue ? await _cardRepo.GetByIdAsync(stock.CardId.Value, ct) : null;
        var displayName = card is not null
            ? await _displayNameResolver.ResolveAsync(card.Id, card.Name, _displayLanguage.Language, ct)
            : null;
        return ShopMapper.ToDto(stock, card, displayName);
    }
}
