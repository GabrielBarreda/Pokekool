using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

public sealed class CardMarketPrice : Entity
{
    public Guid CardId { get; private set; }
    public string Source { get; private set; } = default!;       // "tcgplayer" | "cardmarket"
    public string? Variant { get; private set; }                 // "normal" | "holofoil" | "reverseHolofoil" | null
    public decimal? MarketPrice { get; private set; }
    public decimal? LowPrice { get; private set; }
    public decimal? HighPrice { get; private set; }
    // Prix gradés PSA (source: PriceCharting, PSA, etc.)
    public decimal? Psa7  { get; private set; }
    public decimal? Psa8  { get; private set; }
    public decimal? Psa9  { get; private set; }
    public decimal? Psa95 { get; private set; }   // PSA 9.5 / BGS 9.5
    public decimal? Psa10 { get; private set; }
    public string Currency { get; private set; } = default!;     // "USD" | "EUR"
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
        string currency,
        decimal? psa7  = null,
        decimal? psa8  = null,
        decimal? psa9  = null,
        decimal? psa95 = null,
        decimal? psa10 = null)
    {
        if (cardId == Guid.Empty) throw new ArgumentException("CardId is required", nameof(cardId));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required", nameof(source));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required", nameof(currency));

        return new CardMarketPrice
        {
            Id         = Guid.NewGuid(),
            CardId     = cardId,
            Source     = source.ToLowerInvariant().Trim(),
            Variant    = variant?.Trim(),
            MarketPrice = marketPrice,
            LowPrice   = lowPrice,
            HighPrice  = highPrice,
            Psa7       = psa7,
            Psa8       = psa8,
            Psa9       = psa9,
            Psa95      = psa95,
            Psa10      = psa10,
            Currency   = currency.ToUpperInvariant().Trim(),
            FetchedAt  = DateTime.UtcNow,
            CreatedAt  = DateTimeOffset.UtcNow
        };
    }

    public void UpdatePrices(
        decimal? marketPrice,
        decimal? lowPrice,
        decimal? highPrice,
        decimal? psa7  = null,
        decimal? psa8  = null,
        decimal? psa9  = null,
        decimal? psa95 = null,
        decimal? psa10 = null)
    {
        MarketPrice = marketPrice;
        LowPrice    = lowPrice;
        HighPrice   = highPrice;
        Psa7        = psa7;
        Psa8        = psa8;
        Psa9        = psa9;
        Psa95       = psa95;
        Psa10       = psa10;
        FetchedAt   = DateTime.UtcNow;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }
}
