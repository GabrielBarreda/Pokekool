using MediatR;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetMarketCards;

public sealed class GetMarketCardsQueryHandler : IRequestHandler<GetMarketCardsQuery, PagedResult<MarketCardDto>>
{
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardMarketPriceRepository _priceRepo;

    public GetMarketCardsQueryHandler(
        IPokemonCardRepository cardRepo,
        ICardMarketPriceRepository priceRepo)
    {
        _cardRepo = cardRepo;
        _priceRepo = priceRepo;
    }

    public async Task<PagedResult<MarketCardDto>> Handle(GetMarketCardsQuery request, CancellationToken cancellationToken)
    {
        var (cards, totalCount) = await _cardRepo.SearchAsync(
            request.Search,
            request.SetId,
            request.Rarity,
            request.Supertype,
            request.Page,
            request.PageSize,
            cancellationToken);

        var cardIds = cards.Select(c => c.Id);
        var pricesByCard = await _priceRepo.GetByCardIdsAsync(cardIds, cancellationToken);

        var dtos = cards.Select(card =>
        {
            pricesByCard.TryGetValue(card.Id, out var prices);
            var (market, low, currency, source) = ResolveBestPrice(prices);

            return new MarketCardDto(
                card.Id,
                card.TcgId,
                card.Number,
                card.Name,
                card.Supertype,
                card.Rarity,
                card.Types,
                card.ImageSmall,
                card.ImageLarge,
                market,
                low,
                currency,
                source,
                AlertCount: 0);
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        return new PagedResult<MarketCardDto>(dtos, request.Page, request.PageSize, totalCount, totalPages);
    }

    /// <summary>
    /// Priorité : tcgplayer holofoil > tcgplayer normal > cardmarket.
    /// Retourne (marketPrice, lowPrice, currency, source).
    /// </summary>
    private static (decimal? market, decimal? low, string? currency, string? source) ResolveBestPrice(
        IReadOnlyList<CardMarketPrice>? prices)
    {
        if (prices is null || prices.Count == 0)
            return (null, null, null, null);

        // tcgplayer holofoil
        var tcgHolo = prices.FirstOrDefault(p =>
            p.Source == "tcgplayer" && p.Variant == "holofoil");
        if (tcgHolo?.MarketPrice is not null)
            return (tcgHolo.MarketPrice, tcgHolo.LowPrice, tcgHolo.Currency, "tcgplayer/holofoil");

        // tcgplayer normal
        var tcgNormal = prices.FirstOrDefault(p =>
            p.Source == "tcgplayer" && p.Variant == "normal");
        if (tcgNormal?.MarketPrice is not null)
            return (tcgNormal.MarketPrice, tcgNormal.LowPrice, tcgNormal.Currency, "tcgplayer/normal");

        // cardmarket averageSellPrice (stored as MarketPrice)
        var cm = prices.FirstOrDefault(p => p.Source == "cardmarket");
        if (cm?.MarketPrice is not null)
            return (cm.MarketPrice, cm.LowPrice, cm.Currency, "cardmarket");

        return (null, null, null, null);
    }
}
