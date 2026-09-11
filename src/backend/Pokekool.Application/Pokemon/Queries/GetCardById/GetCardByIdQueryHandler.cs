using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCardById;

public sealed class GetCardByIdQueryHandler : IRequestHandler<GetCardByIdQuery, PokemonCardDto>
{
    private readonly IPokemonCardRepository       _cardRepo;
    private readonly ICardDisplayNameResolver     _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetCardByIdQueryHandler(
        IPokemonCardRepository         cardRepo,
        ICardDisplayNameResolver       displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _cardRepo            = cardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage     = displayLanguage;
    }

    public async Task<PokemonCardDto> Handle(GetCardByIdQuery request, CancellationToken cancellationToken)
    {
        var card = await _cardRepo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw PokemonErrors.CardNotFound(request.Id);

        var displayName = await _displayNameResolver.ResolveAsync(
            card.Id, card.Name, _displayLanguage.Language, cancellationToken);

        return new PokemonCardDto(
            card.Id,
            card.TcgId,
            card.Number,
            card.Name,
            displayName,
            card.Supertype,
            card.Subtypes,
            card.Rarity,
            card.Types,
            card.Hp,
            card.ImageSmall,
            card.ImageLarge,
            card.Set?.Name ?? string.Empty,
            card.Set?.SymbolUrl);
    }
}
