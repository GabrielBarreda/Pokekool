using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetSetFilters;

public sealed class GetSetFiltersQueryHandler : IRequestHandler<GetSetFiltersQuery, SetFiltersDto>
{
    private readonly IPokemonCardRepository _cardRepo;

    public GetSetFiltersQueryHandler(IPokemonCardRepository cardRepo)
    {
        _cardRepo = cardRepo;
    }

    public async Task<SetFiltersDto> Handle(GetSetFiltersQuery request, CancellationToken cancellationToken)
    {
        var (rarities, supertypes) = await _cardRepo.GetDistinctFiltersAsync(request.SetId, cancellationToken);
        return new SetFiltersDto(rarities, supertypes);
    }
}
