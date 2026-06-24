using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

public sealed class PokemonCard : Entity
{
    public string TcgId { get; private set; } = default!;
    public Guid SetId { get; private set; }
    public string Number { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Supertype { get; private set; }
    public string[]? Subtypes { get; private set; }
    public string? Rarity { get; private set; }
    public string[]? Types { get; private set; }
    public int? Hp { get; private set; }
    public string? ImageSmall { get; private set; }
    public string? ImageLarge { get; private set; }
    public bool IsLegal { get; private set; } = true;
    public int? CmProductId { get; private set; }         // ID produit CardMarket (null = non mappé)
    public string? CmScrapingSlug { get; private set; }   // Slug carte CM override (null = généré auto)
    public int? TcgPlayerProductId { get; private set; }  // ID produit TCGPlayer (null = non mappé)

    // Navigation
    public PokemonSet? Set { get; private set; }

    private PokemonCard() { }

    public static PokemonCard Create(
        string tcgId,
        Guid setId,
        string number,
        string name,
        string? supertype,
        string[]? subtypes,
        string? rarity,
        string[]? types,
        int? hp,
        string? imageSmall,
        string? imageLarge)
    {
        if (string.IsNullOrWhiteSpace(tcgId))
            throw new ArgumentException("TcgId is required", nameof(tcgId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new PokemonCard
        {
            Id = Guid.NewGuid(),
            TcgId = tcgId.Trim(),
            SetId = setId,
            Number = number.Trim(),
            Name = name.Trim(),
            Supertype = supertype,
            Subtypes = subtypes,
            Rarity = rarity,
            Types = types,
            Hp = hp,
            ImageSmall = imageSmall,
            ImageLarge = imageLarge,
            IsLegal = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string name,
        string? supertype,
        string[]? subtypes,
        string? rarity,
        string[]? types,
        int? hp,
        string? imageSmall,
        string? imageLarge)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        Name = name.Trim();
        Supertype = supertype;
        Subtypes = subtypes;
        Rarity = rarity;
        Types = types;
        Hp = hp;
        ImageSmall = imageSmall;
        ImageLarge = imageLarge;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCmProductId(int? productId)
    {
        CmProductId = productId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Définit un slug CardMarket manuel pour cette carte.
    /// Null = le slug sera calculé automatiquement depuis le nom de la carte.
    /// </summary>
    public void SetCmScrapingSlug(string? slug)
    {
        CmScrapingSlug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetTcgPlayerProductId(int? productId)
    {
        TcgPlayerProductId = productId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
