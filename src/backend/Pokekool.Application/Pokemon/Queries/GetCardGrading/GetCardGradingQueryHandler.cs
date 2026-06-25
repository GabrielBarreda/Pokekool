using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCardGrading;

public sealed class GetCardGradingQueryHandler
    : IRequestHandler<GetCardGradingQuery, IReadOnlyList<CardGradingDto>>
{
    private readonly ICardMarketPriceRepository _priceRepo;

    public GetCardGradingQueryHandler(ICardMarketPriceRepository priceRepo)
        => _priceRepo = priceRepo;

    public async Task<IReadOnlyList<CardGradingDto>> Handle(
        GetCardGradingQuery request, CancellationToken cancellationToken)
    {
        var prices = await _priceRepo.GetByCardIdAsync(request.CardId, cancellationToken);
        var result = new List<CardGradingDto>();

        // ── PSA via PriceCharting ──────────────────────────────────────────
        var pc = prices.FirstOrDefault(p => p.Source == "pricecharting");
        if (pc is not null)
        {
            var entries = new List<GradeEntryDto>();

            if (pc.Psa7  is not null) entries.Add(new GradeEntryDto(7,    "7",   pc.Psa7,  null, null));
            if (pc.Psa8  is not null) entries.Add(new GradeEntryDto(8,    "8",   pc.Psa8,  null, null));
            if (pc.Psa9  is not null) entries.Add(new GradeEntryDto(9,    "9",   pc.Psa9,  null, null));
            if (pc.Psa95 is not null) entries.Add(new GradeEntryDto(9.5m, "9.5", pc.Psa95, null, null));
            if (pc.Psa10 is not null) entries.Add(new GradeEntryDto(10,   "10",  pc.Psa10, null, null));

            if (entries.Count > 0)
                result.Add(new CardGradingDto("PSA", pc.FetchedAt, entries));
        }

        // ── Autres sociétés à brancher ici quand les providers existent ───
        // BGS, CGC, SGC, AGS, CSG...

        return result;
    }
}
