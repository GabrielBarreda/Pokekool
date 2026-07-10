using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Shop;

/// <summary>
/// Stock interne de l'entité morale — tout ce que la boutique possède,
/// pas uniquement ce qui est exposé en vente.
/// </summary>
public sealed class ShopStock : Entity
{
    public Guid ShopId { get; private set; }
    public Guid? CardId { get; private set; }
    public string? ProductName { get; private set; }
    public ShopProductType Type { get; private set; }
    public string? Condition { get; private set; }
    public string? GradingCompany { get; private set; }
    public decimal? GradeValue { get; private set; }

    public int QuantityTotal { get; private set; }
    public int QuantityAvailable { get; private set; }
    public int QuantityReserved { get; private set; }

    public decimal? PurchasePrice { get; private set; }
    public string? PurchaseCurrency { get; private set; }
    public string? Notes { get; private set; }

    private ShopStock() { }

    public static ShopStock Create(
        Guid shopId,
        Guid? cardId,
        string? productName,
        ShopProductType type,
        string? condition,
        string? gradingCompany,
        decimal? gradeValue,
        int quantityTotal,
        int quantityAvailable,
        int quantityReserved,
        decimal? purchasePrice,
        string? purchaseCurrency,
        string? notes)
    {
        var stock = new ShopStock
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        stock.Apply(cardId, productName, type, condition, gradingCompany, gradeValue,
            quantityTotal, quantityAvailable, quantityReserved, purchasePrice, purchaseCurrency, notes);
        return stock;
    }

    public void Update(
        Guid? cardId,
        string? productName,
        ShopProductType type,
        string? condition,
        string? gradingCompany,
        decimal? gradeValue,
        int quantityTotal,
        int quantityAvailable,
        int quantityReserved,
        decimal? purchasePrice,
        string? purchaseCurrency,
        string? notes)
    {
        Apply(cardId, productName, type, condition, gradingCompany, gradeValue,
            quantityTotal, quantityAvailable, quantityReserved, purchasePrice, purchaseCurrency, notes);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(
        Guid? cardId,
        string? productName,
        ShopProductType type,
        string? condition,
        string? gradingCompany,
        decimal? gradeValue,
        int quantityTotal,
        int quantityAvailable,
        int quantityReserved,
        decimal? purchasePrice,
        string? purchaseCurrency,
        string? notes)
    {
        if (quantityTotal < 0 || quantityAvailable < 0 || quantityReserved < 0)
            throw new ArgumentException("Quantities must be >= 0");
        if (cardId is null && string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("CardId or ProductName is required", nameof(productName));

        CardId = cardId;
        ProductName = string.IsNullOrWhiteSpace(productName) ? null : productName.Trim();
        Type = type;
        Condition = condition;
        GradingCompany = gradingCompany;
        GradeValue = gradeValue;
        QuantityTotal = quantityTotal;
        QuantityAvailable = quantityAvailable;
        QuantityReserved = quantityReserved;
        PurchasePrice = purchasePrice;
        PurchaseCurrency = string.IsNullOrWhiteSpace(purchaseCurrency) ? null : purchaseCurrency.Trim().ToUpperInvariant();
        Notes = notes;
    }
}
