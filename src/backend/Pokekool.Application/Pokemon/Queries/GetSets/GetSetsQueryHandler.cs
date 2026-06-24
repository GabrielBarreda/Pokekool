using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSets;

public sealed class GetSetsQueryHandler : IRequestHandler<GetSetsQuery, IReadOnlyList<PokemonSetDto>>
{
    private readonly IPokemonSetRepository _setRepo;

    public GetSetsQueryHandler(IPokemonSetRepository setRepo)
    {
        _setRepo = setRepo;
    }

    public async Task<IReadOnlyList<PokemonSetDto>> Handle(GetSetsQuery request, CancellationToken cancellationToken)
    {
        var sets = await _setRepo.ListAsync(cancellationToken);

        var result = sets.AsEnumerable();
        if (request.SeriesId.HasValue)
            result = result.Where(s => s.SeriesId == request.SeriesId.Value);

        return result
            .Select(s => new PokemonSetDto(
                s.Id,
                s.TcgId,
                s.Name,
                s.Series,
                s.ReleaseDate,
                s.TotalCards,
                s.PrintedTotal,
                s.SymbolUrl,
                s.LogoUrl,
                s.Cards.Count))
            .ToList();
    }
}
