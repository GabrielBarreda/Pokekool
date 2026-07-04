using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Application.Pricing;

/// <summary>
/// Calcule un prix agrégé pondéré à partir de plusieurs sources de données.
///
/// Deux flux d'entrée :
///   • <c>rawPrices</c>    — CardMarketPrice  (prix brut non-gradé par source)
///   • <c>gradedPrices</c> — CardGradedPrice  (prix par société + grade + source)
///
/// Pour chaque (société, grade), les valeurs de toutes les sources sont moyennées
/// en pondérant par le poids de confiance de la source.
///
/// Utilisation :
///   var raw    = await _priceRepo.GetByCardIdAsync(cardId);
///   var graded = await _gradedPriceRepo.GetByCardIdAsync(cardId);
///   var result = PriceAggregator.Compute(raw, graded);
///   // result.MarketPrice      = moyenne pondérée du prix brut
///   // result.AllGradedPrices  = { "PSA" → { 10 → 25000 }, "BGS" → { 9.5 → 84000 } }
///   // result.Breakdown        = détail par source (pour tooltip frontend)
/// </summary>
public static class PriceAggregator
{
    private static readonly Dictionary<string, double> SourceWeights =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pricecharting"] = 1.00,
            ["tcgplayer"]     = 1.00,
            ["cardladder"]    = 0.95,
            ["cardmarket"]    = 0.90,
            ["130point"]      = 0.80,
            ["cardtrader"]    = 0.70,
        };

    private const double DefaultWeight = 0.75;

    // ── API publique ──────────────────────────────────────────────────────────

    public static AggregatedCardPrice Compute(
        IReadOnlyList<CardMarketPrice> rawPrices,
        IReadOnlyList<CardGradedPrice> gradedPrices)
    {
        if (rawPrices.Count == 0 && gradedPrices.Count == 0)
            return AggregatedCardPrice.Empty;

        // ── Prix brut agrégé ─────────────────────────────────────────────────
        var marketPrice = WeightedAvgRaw(rawPrices);

        // ── Prix gradés agrégés par (société, grade) ─────────────────────────
        var byKey = gradedPrices
            .GroupBy(g => (g.GradingCompany, g.GradeValue));

        var allGraded = new Dictionary<string, Dictionary<decimal, decimal>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in byKey)
        {
            var (company, grade) = group.Key;
            var price = WeightedAvgGraded(group);
            if (price is null) continue;

            if (!allGraded.TryGetValue(company, out var companyDict))
            {
                companyDict = [];
                allGraded[company] = companyDict;
            }
            companyDict[grade] = price.Value;
        }

        // Freeze dictionaries
        var frozen = allGraded.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyDictionary<decimal, decimal>)kv.Value,
            StringComparer.OrdinalIgnoreCase);

        // ── Breakdown par source ─────────────────────────────────────────────
        var allSources = rawPrices.Select(p => p.Source)
            .Concat(gradedPrices.Select(g => g.Source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var breakdown = allSources.Select(source =>
        {
            var rawForSource     = rawPrices.FirstOrDefault(p => p.Source == source);
            var gradedForSource  = gradedPrices.Where(g => g.Source == source).ToList();
            var latestFetchedAt  = rawForSource?.FetchedAt
                ?? (gradedForSource.Count > 0 ? gradedForSource.Max(g => g.FetchedAt) : DateTime.UtcNow);

            return new SourcePrice(
                Source:        source,
                MarketPrice:   rawForSource?.MarketPrice,
                GradedPrices:  gradedForSource
                    .Select(g => new GradedSourceEntry(g.GradingCompany, g.GradeValue, g.Price))
                    .ToList(),
                Weight:        GetWeight(source),
                FetchedAt:     latestFetchedAt);
        }).ToList();

        return new AggregatedCardPrice(marketPrice, frozen, breakdown);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static decimal? WeightedAvgRaw(IReadOnlyList<CardMarketPrice> prices)
    {
        var contributions = prices
            .Select(p => (Value: p.MarketPrice, Weight: GetWeight(p.Source)))
            .Where(x => x.Value.HasValue && x.Value.Value > 0)
            .ToList();

        return WAvg(contributions);
    }

    private static decimal? WeightedAvgGraded(IEnumerable<CardGradedPrice> group)
    {
        var contributions = group
            .Select(g => (Value: (decimal?)g.Price, Weight: GetWeight(g.Source)))
            .Where(x => x.Value.HasValue && x.Value.Value > 0)
            .ToList();

        return WAvg(contributions);
    }

    private static decimal? WAvg(List<(decimal? Value, double Weight)> contributions)
    {
        if (contributions.Count == 0) return null;
        if (contributions.Count == 1) return contributions[0].Value;

        var totalWeight = contributions.Sum(x => x.Weight);
        var weightedSum = contributions.Sum(x => (double)x.Value!.Value * x.Weight);

        return Math.Round((decimal)(weightedSum / totalWeight), 2);
    }

    private static double GetWeight(string source)
        => SourceWeights.TryGetValue(source, out var w) ? w : DefaultWeight;
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Prix agrégé toutes sources + détail pour tooltip.</summary>
public sealed record AggregatedCardPrice(
    decimal? MarketPrice,
    /// <summary>
    /// Prix gradés agrégés : Company → (GradeValue → Prix moyen pondéré).
    /// Ex : allGradedPrices["PSA"][10m] = 25000m
    ///      allGradedPrices["BGS"][9.5m] = 84000m
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyDictionary<decimal, decimal>> AllGradedPrices,
    IReadOnlyList<SourcePrice> Breakdown)
{
    public static readonly AggregatedCardPrice Empty =
        new(null,
            new Dictionary<string, IReadOnlyDictionary<decimal, decimal>>(),
            []);

    public bool HasAnyPrice =>
        MarketPrice is not null || AllGradedPrices.Count > 0;

    /// <summary>Raccourci : prix PSA par grade.</summary>
    public decimal? Psa(decimal grade)
        => AllGradedPrices.TryGetValue("PSA", out var d) && d.TryGetValue(grade, out var p) ? p : null;
}

/// <summary>Contribution d'une source individuelle.</summary>
public sealed record SourcePrice(
    string   Source,
    decimal? MarketPrice,
    IReadOnlyList<GradedSourceEntry> GradedPrices,
    double   Weight,
    DateTime FetchedAt);

/// <summary>Un grade précis dans le breakdown d'une source.</summary>
public sealed record GradedSourceEntry(
    string  GradingCompany,
    decimal GradeValue,
    decimal Price);
