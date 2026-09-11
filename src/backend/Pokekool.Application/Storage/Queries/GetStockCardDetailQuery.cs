using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

/// <summary>
/// Détail "comptoir" d'un item de stock rangé (grade/condition, prix estimé, valeurs récentes même grade).
/// </summary>
public sealed record GetStockCardDetailQuery(string OwnerType, Guid OwnerId) : IRequest<StorageCardDetailDto>;

internal sealed class GetStockCardDetailQueryHandler
    : IRequestHandler<GetStockCardDetailQuery, StorageCardDetailDto>
{
    private readonly IShopStockRepository _stockRepo;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardMarketPriceRepository _marketRepo;
    private readonly ICardGradedPriceRepository _gradedRepo;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetStockCardDetailQueryHandler(
        IShopStockRepository stockRepo, IPokemonCardRepository cardRepo,
        ICardMarketPriceRepository marketRepo, ICardGradedPriceRepository gradedRepo,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _stockRepo = stockRepo;
        _cardRepo = cardRepo;
        _marketRepo = marketRepo;
        _gradedRepo = gradedRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
    }

    public async Task<StorageCardDetailDto> Handle(GetStockCardDetailQuery request, CancellationToken ct)
    {
        if (request.OwnerType != StorageLocation.OwnerShopStock)
            throw new DomainException("storage.card.unsupported_owner", "Détail disponible uniquement pour le stock boutique.");

        var stock = await _stockRepo.GetByIdAsync(request.OwnerId, ct)
            ?? throw new DomainException("storage.card.not_found", "Article de stock introuvable.");

        var card = stock.CardId is { } cid ? await _cardRepo.GetByIdAsync(cid, ct) : null;
        var name = card?.Name ?? stock.ProductName ?? "Article";
        // DisplayName n'a de sens que pour une carte du référentiel (card non null) : sinon
        // (article scellé/sans carte liée), pas de traduction possible, on garde name tel quel.
        var displayName = card is not null
            ? await _displayNameResolver.ResolveAsync(card.Id, card.Name, _displayLanguage.Language, ct)
            : name;

        decimal? estimated = null;
        string? currency = null;
        var recent = new List<StoragePricePointDto>();

        if (card is not null)
        {
            var isGraded = !string.IsNullOrWhiteSpace(stock.GradingCompany) && stock.GradeValue is not null;
            if (isGraded)
            {
                var graded = await _gradedRepo.GetByCardIdAsync(card.Id, ct);
                var same = graded
                    .Where(g => string.Equals(g.GradingCompany, stock.GradingCompany, StringComparison.OrdinalIgnoreCase)
                                && g.GradeValue == stock.GradeValue!.Value)
                    .OrderByDescending(g => g.FetchedAt)
                    .ToList();
                estimated = same.FirstOrDefault()?.Price;
                currency = same.FirstOrDefault()?.Currency;
                recent = same.Take(10)
                    .Select(g => new StoragePricePointDto(g.Source, g.Price, g.Currency, g.FetchedAt))
                    .ToList();
            }
            else
            {
                var market = (await _marketRepo.GetByCardIdAsync(card.Id, ct))
                    .Where(m => m.MarketPrice.HasValue)
                    .OrderByDescending(m => m.FetchedAt)
                    .ToList();
                estimated = market.FirstOrDefault()?.MarketPrice;
                currency = market.FirstOrDefault()?.Currency;
                recent = market.Take(10)
                    .Select(m => new StoragePricePointDto(m.Source, m.MarketPrice!.Value, m.Currency, m.FetchedAt))
                    .ToList();
            }
        }

        return new StorageCardDetailDto(
            card?.Id, card?.TcgId, name, displayName, card?.Set?.Name, card?.ImageSmall, card?.ImageLarge,
            stock.Type.ToString(), stock.Condition, stock.GradingCompany, stock.GradeValue,
            estimated, currency, stock.QuantityAvailable, recent);
    }
}
