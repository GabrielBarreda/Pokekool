using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSetById;

public sealed class GetSetByIdQueryHandler : IRequestHandler<GetSetByIdQuery, PokemonSetDto>
{
    private readonly IPokemonSetRepository _setRepo;

    public GetSetByIdQueryHandler(IPokemonSetRepository setRepo)
    {
        _setRepo = setRepo;
    }

    public async Task<PokemonSetDto> Handle(GetSetByIdQuery request, CancellationToken cancellationToken)
    {
        var set = await _setRepo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw PokemonErrors.SetNotFound(request.Id);

        return new PokemonSetDto(
            set.Id,
            set.TcgId,
            set.Name,
            set.Series,
            set.ReleaseDate,
            set.TotalCards,
            set.PrintedTotal,
            set.SymbolUrl,
            set.LogoUrl,
            set.Cards.Count);
    }
}
