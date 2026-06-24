using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

public sealed class PokemonSet : Entity
{
    public string TcgId { get; private set; } = default!;
    public Guid SeriesId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Series { get; private set; }
    public DateOnly? ReleaseDate { get; private set; }
    public int? TotalCards { get; private set; }
    public int? PrintedTotal { get; private set; }
    public string? SymbolUrl { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PriceChartingSlug { get; private set; }   // ex: "pokemon-base-set"
    public int? CmExpansionId { get; private set; }          // ID expansion CardMarket
    public int? TcgPlayerGroupId { get; private set; }       // Group ID TCGPlayer (null = non mappé)

    // Navigation
    public PokemonSeries? SeriesEntity { get; private set; }

    private readonly List<PokemonCard> _cards = [];
    public IReadOnlyCollection<PokemonCard> Cards => _cards.AsReadOnly();

    private PokemonSet() { }

    public static PokemonSet Create(
        string tcgId,
        Guid seriesId,
        string name,
        string? series,
        DateOnly? releaseDate,
        int? totalCards,
        int? printedTotal,
        string? symbolUrl,
        string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(tcgId))
            throw new ArgumentException("TcgId is required", nameof(tcgId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new PokemonSet
        {
            Id = Guid.NewGuid(),
            TcgId = tcgId.Trim(),
            SeriesId = seriesId,
            Name = name.Trim(),
            Series = series,
            ReleaseDate = releaseDate,
            TotalCards = totalCards,
            PrintedTotal = printedTotal,
            SymbolUrl = symbolUrl,
            LogoUrl = logoUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string name,
        DateOnly? releaseDate,
        int? totalCards,
        int? printedTotal,
        string? symbolUrl,
        string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        Name = name.Trim();
        ReleaseDate = releaseDate;
        TotalCards = totalCards;
        PrintedTotal = printedTotal;
        SymbolUrl = symbolUrl;
        LogoUrl = logoUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPriceChartingSlug(string? slug)
    {
        PriceChartingSlug = slug?.Trim().ToLowerInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCmExpansionId(int? expansionId)
    {
        CmExpansionId = expansionId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetTcgPlayerGroupId(int? groupId)
    {
        TcgPlayerGroupId = groupId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
