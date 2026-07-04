using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

/// <summary>
/// Prix d'une carte pour un grade précis d'une société de notation.
/// Remplace les colonnes psa1..psa10 de <c>card_market_prices</c>.
/// </summary>
public sealed class CardGradedPrice : Entity
{
    public Guid    CardId         { get; private set; }
    public string  Source         { get; private set; } = default!;  // "pricecharting", "cardladder"…
    public string  GradingCompany { get; private set; } = default!;  // "PSA", "BGS", "CGC", "SGC"
    public decimal GradeValue     { get; private set; }               // 1.0, 9.5, 10.0
    public decimal Price          { get; private set; }
    public string  Currency       { get; private set; } = default!;
    public DateTime FetchedAt     { get; private set; }

    // Navigation
    public PokemonCard? Card { get; private set; }

    private CardGradedPrice() { }

    public static CardGradedPrice Create(
        Guid    cardId,
        string  source,
        string  gradingCompany,
        decimal gradeValue,
        decimal price,
        string  currency = "USD")
    {
        if (cardId == Guid.Empty)
            throw new ArgumentException("CardId required", nameof(cardId));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source required", nameof(source));
        if (string.IsNullOrWhiteSpace(gradingCompany))
            throw new ArgumentException("GradingCompany required", nameof(gradingCompany));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency required", nameof(currency));

        return new CardGradedPrice
        {
            Id             = Guid.NewGuid(),
            CardId         = cardId,
            Source         = source.ToLowerInvariant().Trim(),
            GradingCompany = gradingCompany.ToUpperInvariant().Trim(),
            GradeValue     = gradeValue,
            Price          = price,
            Currency       = currency.ToUpperInvariant().Trim(),
            FetchedAt      = DateTime.UtcNow,
            CreatedAt      = DateTimeOffset.UtcNow,
        };
    }

    public void UpdatePrice(decimal price)
    {
        Price     = price;
        FetchedAt = DateTime.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
