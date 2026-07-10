using Pokekool.Domain.Common;
using Pokekool.Domain.Entities;

namespace Pokekool.Domain.Entities.Shop;

public enum PurchaseRequestStatus
{
    Pending = 0,
    Contacted = 1,
    Completed = 2,
    Cancelled = 3,
}

/// <summary>
/// Demande d'achat émise par un client authentifié sur un article du présentoir (ShopProduct).
/// Aucune transaction financière : la boutique recontacte le client pour une remise en main propre.
/// Nom/email du client dénormalisés au moment de la création (constraint module Client #3).
/// </summary>
public sealed class PurchaseRequest : Entity
{
    public Guid ShopProductId { get; private set; }
    public ShopProduct? ShopProduct { get; private set; }

    public Guid UserId { get; private set; }
    public User? User { get; private set; }

    // Dénormalisation volontaire pour l'affichage admin (le compte peut changer ensuite).
    public string ClientName { get; private set; } = default!;
    public string ClientEmail { get; private set; } = default!;

    public string? Message { get; private set; }
    public PurchaseRequestStatus Status { get; private set; } = PurchaseRequestStatus.Pending;

    private PurchaseRequest() { }

    public static PurchaseRequest Create(
        Guid shopProductId,
        Guid userId,
        string clientName,
        string clientEmail,
        string? message)
    {
        if (shopProductId == Guid.Empty) throw new ArgumentException("ShopProductId is required", nameof(shopProductId));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientEmail)) throw new ArgumentException("ClientEmail is required", nameof(clientEmail));

        return new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            ShopProductId = shopProductId,
            UserId = userId,
            ClientName = clientName?.Trim() ?? string.Empty,
            ClientEmail = clientEmail.Trim(),
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            Status = PurchaseRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void ChangeStatus(PurchaseRequestStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
