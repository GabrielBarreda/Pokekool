using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCards;

public sealed class GetCardsQueryHandler : IRequestHandler<GetCardsQuery, PagedResult<PokemonCardSummaryDto>>
{
    private readonly IPokemonCardRepository _cardRepo;

    public GetCardsQueryHandler(IPokemonCardRepository cardRepo)
    {
        _cardRepo = cardRepo;
    }

    public async Task<PagedResult<PokemonCardSummaryDto>> Handle(GetCardsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _cardRepo.SearchAsync(
            request.Search,
            request.SetId,
            request.Rarity,
            request.Supertype,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items
            .Select(c => new PokemonCardSummaryDto(
                c.Id,
                c.TcgId,
                c.Number,
                c.Name,
                c.Rarity,
                c.ImageSmall,
                c.Set?.Name ?? string.Empty,
                c.Set?.TcgId ?? string.Empty))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PagedResult<PokemonCardSummaryDto>(dtos, request.Page, request.PageSize, totalCount, totalPages);
    }
}
