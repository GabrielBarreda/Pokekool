using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

public sealed class CardPopulation : Entity
{
    public Guid CardId { get; private set; }
    public string Source { get; private set; } = default!;   // "psa" | "cgc" | "pca"
    public int? TotalGraded { get; private set; }
    public int? Pop10 { get; private set; }
    public int? Pop9 { get; private set; }
    public int? Pop8 { get; private set; }
    public int? Pop7 { get; private set; }
    public double? GemRate10 { get; private set; }           // Pop10 / Total * 100
    public DateTime FetchedAt { get; private set; }

    // Navigation
    public PokemonCard? Card { get; private set; }

    private CardPopulation() { }

    public static CardPopulation Create(
        Guid cardId,
        string source,
        int? totalGraded,
        int? pop10,
        int? pop9,
        int? pop8,
        int? pop7,
        double? gemRate10)
    {
        if (cardId == Guid.Empty) throw new ArgumentException("CardId is required", nameof(cardId));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required", nameof(source));

        return new CardPopulation
        {
            Id          = Guid.NewGuid(),
            CardId      = cardId,
            Source      = source.ToLowerInvariant().Trim(),
            TotalGraded = totalGraded,
            Pop10       = pop10,
            Pop9        = pop9,
            Pop8        = pop8,
            Pop7        = pop7,
            GemRate10   = gemRate10,
            FetchedAt   = DateTime.UtcNow,
            CreatedAt   = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        int? totalGraded,
        int? pop10,
        int? pop9,
        int? pop8,
        int? pop7,
        double? gemRate10)
    {
        TotalGraded = totalGraded;
        Pop10       = pop10;
        Pop9        = pop9;
        Pop8        = pop8;
        Pop7        = pop7;
        GemRate10   = gemRate10;
        FetchedAt   = DateTime.UtcNow;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }
}
