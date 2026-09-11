using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCards;

public sealed class GetCardsQueryHandler : IRequestHandler<GetCardsQuery, PagedResult<PokemonCardSummaryDto>>
{
    private readonly IPokemonCardRepository         _cardRepo;
    private readonly ICardDisplayNameResolver       _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetCardsQueryHandler(
        IPokemonCardRepository         cardRepo,
        ICardDisplayNameResolver       displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _cardRepo            = cardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage     = displayLanguage;
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

        var englishNamesByCardId = items.ToDictionary(c => c.Id, c => c.Name);
        var displayNames = await _displayNameResolver.ResolveManyAsync(
            englishNamesByCardId, _displayLanguage.Language, cancellationToken);

        var dtos = items
            .Select(c => new PokemonCardSummaryDto(
                c.Id,
                c.TcgId,
                c.Number,
                c.Name,
                displayNames.TryGetValue(c.Id, out var displayName) ? displayName : c.Name,
                c.Rarity,
                c.ImageSmall,
                c.Set?.Name ?? string.Empty,
                c.Set?.TcgId ?? string.Empty))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PagedResult<PokemonCardSummaryDto>(dtos, request.Page, request.PageSize, totalCount, totalPages);
    }
}
