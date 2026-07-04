using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

public sealed class CardMarketPrice : Entity
{
    public Guid CardId { get; private set; }
    public string Source { get; private set; } = default!;    // "pricecharting" | "cardmarket" | …
    public string? Variant { get; private set; }               // "normal" | "holofoil" | null
    public decimal? MarketPrice { get; private set; }
    public decimal? LowPrice { get; private set; }
    public decimal? HighPrice { get; private set; }
    public string Currency { get; private set; } = default!;  // "USD" | "EUR"
    public DateTime FetchedAt { get; private set; }

    // Navigation
    public PokemonCard? Card { get; private set; }

    private CardMarketPrice() { }

    public static CardMarketPrice Create(
        Guid cardId,
        string source,
        string? variant,
        decimal? marketPrice,
        decimal? lowPrice,
        decimal? highPrice,
        string currency)
    {
        if (cardId == Guid.Empty) throw new ArgumentException("CardId is required", nameof(cardId));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required", nameof(source));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required", nameof(currency));

        return new CardMarketPrice
        {
            Id          = Guid.NewGuid(),
            CardId      = cardId,
            Source      = source.ToLowerInvariant().Trim(),
            Variant     = variant?.Trim(),
            MarketPrice = marketPrice,
            LowPrice    = lowPrice,
            HighPrice   = highPrice,
            Currency    = currency.ToUpperInvariant().Trim(),
            FetchedAt   = DateTime.UtcNow,
            CreatedAt   = DateTimeOffset.UtcNow
        };
    }

    public void UpdatePrices(
        decimal? marketPrice,
        decimal? lowPrice,
        decimal? highPrice)
    {
        MarketPrice = marketPrice;
        LowPrice    = lowPrice;
        HighPrice   = highPrice;
        FetchedAt   = DateTime.UtcNow;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }
}
