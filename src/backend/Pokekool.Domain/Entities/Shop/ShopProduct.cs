using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Shop;

/// <summary>
/// Article actuellement en vente sur le présentoir de la boutique.
/// </summary>
public sealed class ShopProduct : Entity
{
    public Guid ShopId { get; private set; }
    public ShopProductType Type { get; private set; }

    /// <summary>FK optionnelle vers pokemon_cards (null pour les scellés sans référence carte).</summary>
    public Guid? CardId { get; private set; }

    /// <summary>Nom libre — utilisé pour les scellés sans carte référencée.</summary>
    public string? ProductName { get; private set; }

    /// <summary>État de la carte (RAW uniquement) : NM/LP/MP/HP.</summary>
    public string? Condition { get; private set; }

    /// <summary>Société de gradation (cartes gradées) : PSA/BGS/CGC.</summary>
    public string? GradingCompany { get; private set; }
    public decimal? GradeValue { get; private set; }

    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public int Quantity { get; private set; }
    public bool IsAvailable { get; private set; }

    public string? Description { get; private set; }
    public string? PhotoUrl { get; private set; }

    private ShopProduct() { }

    public static ShopProduct Create(
        Guid shopId,
        ShopProductType type,
        Guid? cardId,
        string? productName,
        string? condition,
        string? gradingCompany,
        decimal? gradeValue,
        decimal price,
        string currency,
        int quantity,
        bool isAvailable,
        string? description,
        string? photoUrl)
    {
        var product = new ShopProduct
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        product.Apply(type, cardId, productName, condition, gradingCompany, gradeValue,
            price, currency, quantity, isAvailable, description, photoUrl);
        return product;
    }

    public void Update(
        ShopProductType type,
        Guid? cardId,
        string? productName,
        string? condition,
        string? gradingCompany,
        decimal? gradeValue,
        decimal price,
        string currency,
        int quantity,
        bool isAvailable,
        string? description,
        string? photoUrl)
    {
        Apply(type, cardId, productName, condition, gradingCompany, gradeValue,
            price, currency, quantity, isAvailable, description, photoUrl);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Bascule la disponibilité. Forcée à false si le stock est nul.</summary>
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = Quantity <= 0 ? false : isAvailable;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(
        ShopProductType type,
        Guid? cardId,
        string? productName,
        string? condition,
        string? gradingCompany,
        decimal? gradeValue,
        decimal price,
        string currency,
        int quantity,
        bool isAvailable,
        string? description,
        string? photoUrl)
    {
        if (price < 0) throw new ArgumentException("Price must be >= 0", nameof(price));
        if (quantity < 0) throw new ArgumentException("Quantity must be >= 0", nameof(quantity));
        if (cardId is null && string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("CardId or ProductName is required", nameof(productName));

        Type = type;
        CardId = cardId;
        ProductName = string.IsNullOrWhiteSpace(productName) ? null : productName.Trim();
        Condition = condition;
        GradingCompany = gradingCompany;
        GradeValue = gradeValue;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();
        Quantity = quantity;
        // Règle métier : quantité nulle => indisponible automatiquement.
        IsAvailable = quantity <= 0 ? false : isAvailable;
        Description = description;
        PhotoUrl = photoUrl;
    }
}
