using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateShopProduct;

internal sealed class UpdateShopProductCommandHandler
    : IRequestHandler<UpdateShopProductCommand, ShopProductDto>
{
    private readonly IShopProductRepository _repo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;
    private readonly IUnitOfWork _uow;

    public UpdateShopProductCommandHandler(
        IShopProductRepository repo, IPokemonCardRepository cardRepo,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage,
        IUnitOfWork uow)
    {
        _repo = repo;
        _cardRepo = cardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
        _uow = uow;
    }

    public async Task<ShopProductDto> Handle(UpdateShopProductCommand request, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.ProductNotFound(request.Id);

        product.Update(
            ShopEnumMap.ParseProductType(request.Type),
            request.CardId, request.ProductName,
            request.Language,
            request.Condition, request.GradingCompany,
            request.GradeValue, request.Price, request.Currency ?? "EUR", request.Quantity,
            request.IsAvailable, request.Description, request.PhotoUrl);

        await _uow.SaveChangesAsync(ct);

        var card = product.CardId.HasValue ? await _cardRepo.GetByIdAsync(product.CardId.Value, ct) : null;
        var displayName = card is not null
            ? await _displayNameResolver.ResolveAsync(card.Id, card.Name, _displayLanguage.Language, ct)
            : null;
        return ShopMapper.ToDto(product, card, displayName);
    }
}
