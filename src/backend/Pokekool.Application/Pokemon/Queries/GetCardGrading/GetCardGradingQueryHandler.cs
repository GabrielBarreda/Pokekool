using MediatR;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCardGrading;

public sealed class GetCardGradingQueryHandler
    : IRequestHandler<GetCardGradingQuery, IReadOnlyList<CardGradingDto>>
{
    private readonly ICardGradedPriceRepository _gradedPriceRepo;
    private readonly ICardPopulationRepository  _populationRepo;

    public GetCardGradingQueryHandler(
        ICardGradedPriceRepository gradedPriceRepo,
        ICardPopulationRepository  populationRepo)
    {
        _gradedPriceRepo = gradedPriceRepo;
        _populationRepo  = populationRepo;
    }

    public async Task<IReadOnlyList<CardGradingDto>> Handle(
        GetCardGradingQuery request, CancellationToken cancellationToken)
    {
        var gradedPrices = await _gradedPriceRepo.GetByCardIdAsync(request.CardId, cancellationToken);
        var populations  = await _populationRepo.GetByCardIdAsync(request.CardId, cancellationToken);

        // ── 1. Index population par company ──────────────────────────────────
        // Plusieurs sources peuvent mapper sur la même company (ex: "psa" et "gemtracker-psa" → PSA)
        // On garde la plus récente par company
        var popByCompany = populations
            .GroupBy(p => MapSourceToCompany(p.Source), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.FetchedAt).First(),
                StringComparer.OrdinalIgnoreCase);

        // ── 2. Construire les DTOs à partir des prix ──────────────────────────
        var byCompany = gradedPrices
            .GroupBy(g => g.GradingCompany, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key);

        var result = new List<CardGradingDto>();

        foreach (var group in byCompany)
        {
            popByCompany.TryGetValue(group.Key, out var pop);

            var entries = group
                .GroupBy(g => g.GradeValue)
                .OrderByDescending(g => g.Key)
                .Select(gGrade =>
                {
                    var best = gGrade.OrderByDescending(g => g.FetchedAt).First();
                    return new GradeEntryDto(
                        Grade:       best.GradeValue,
                        Label:       best.GradeValue.ToString("0.#"),
                        MarketPrice: best.Price,
                        LowPrice:    null,
                        Population:  pop is not null ? GradePopulation(pop, (int)best.GradeValue) : null,
                        Source:      best.Source,
                        Currency:    best.Currency);
                })
                .ToList();

            if (entries.Count > 0)
            {
                var latestFetch = new[] { group.Max(g => g.FetchedAt), pop?.FetchedAt ?? DateTime.MinValue }.Max();
                result.Add(new CardGradingDto(group.Key, latestFetch, entries));
                popByCompany.Remove(group.Key); // déjà consommé
            }
        }

        // ── 3. Companies avec population seulement (pas de prix) ─────────────
        // ex: gemtracker-bgs avant que les prix BGS soient disponibles
        foreach (var (company, pop) in popByCompany.OrderBy(kvp => kvp.Key))
        {
            var entries = new List<GradeEntryDto>();
            foreach (var (grade, popVal) in GradePopulations(pop))
            {
                if (popVal is null) continue;
                entries.Add(new GradeEntryDto(
                    Grade:       grade,
                    Label:       grade.ToString(),
                    MarketPrice: null,
                    LowPrice:    null,
                    Population:  popVal,
                    Source:      pop.Source));
            }

            if (entries.Count > 0)
                result.Add(new CardGradingDto(company, pop.FetchedAt, entries));
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Mappe le source DB vers l'identifiant de société affiché.</summary>
    private static string MapSourceToCompany(string source) =>
        source.ToLowerInvariant() switch
        {
            "psa" or "gemtracker-psa" => "PSA",
            "bgs" or "gemtracker-bgs" => "BGS",
            "cgc"                     => "CGC",
            "sgc"                     => "SGC",
            "ags"                     => "AGS",
            "csg"                     => "CSG",
            _                         => source.ToUpperInvariant()
        };

    /// <summary>Population pour un grade précis depuis une entrée CardPopulation.</summary>
    private static int? GradePopulation(CardPopulation p, int grade) => grade switch
    {
        10 => p.Pop10,
        9  => p.Pop9,
        8  => p.Pop8,
        7  => p.Pop7,
        _  => null
    };

    /// <summary>Toutes les paires (grade, population) non-nulles d'une entrée.</summary>
    private static IEnumerable<(int Grade, int? Pop)> GradePopulations(CardPopulation p)
    {
        yield return (10, p.Pop10);
        yield return (9,  p.Pop9);
        yield return (8,  p.Pop8);
        yield return (7,  p.Pop7);
    }
}
