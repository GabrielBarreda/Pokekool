using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCardById;

public sealed class GetCardByIdQueryHandler : IRequestHandler<GetCardByIdQuery, PokemonCardDto>
{
    private readonly IPokemonCardRepository _cardRepo;

    public GetCardByIdQueryHandler(IPokemonCardRepository cardRepo)
    {
        _cardRepo = cardRepo;
    }

    public async Task<PokemonCardDto> Handle(GetCardByIdQuery request, CancellationToken cancellationToken)
    {
        var card = await _cardRepo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw PokemonErrors.CardNotFound(request.Id);

        return new PokemonCardDto(
            card.Id,
            card.TcgId,
            card.Number,
            card.Name,
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
