using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSeries;

public sealed class GetSeriesQueryHandler : IRequestHandler<GetSeriesQuery, IReadOnlyList<PokemonSeriesDto>>
{
    private readonly IPokemonSeriesRepository _seriesRepo;

    public GetSeriesQueryHandler(IPokemonSeriesRepository seriesRepo)
    {
        _seriesRepo = seriesRepo;
    }

    public async Task<IReadOnlyList<PokemonSeriesDto>> Handle(GetSeriesQuery request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepo.ListAsync(cancellationToken);

        return series
            .Select(s => new PokemonSeriesDto(
                s.Id,
                s.TcgId,
                s.Name,
                s.LogoUrl,
                s.Sets.Count))
            .ToList();
    }
}
