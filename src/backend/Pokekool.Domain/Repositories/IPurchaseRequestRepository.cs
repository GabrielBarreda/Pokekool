using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Domain.Repositories;

public interface IPurchaseRequestRepository
{
    Task AddAsync(PurchaseRequest request, CancellationToken ct = default);

    Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Liste paginée pour l'admin, filtrable par statut.</summary>
    Task<(IReadOnlyList<PurchaseRequestProjection> Items, long TotalCount)> GetPagedAsync(
        PurchaseRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

/// <summary>Projection plate jointe au ShopProduct pour l'affichage admin.</summary>
public sealed record PurchaseRequestProjection(
    Guid Id,
    Guid ShopProductId,
    string? ProductName,
    decimal ProductPrice,
    string ProductCurrency,
    Guid UserId,
    string ClientName,
    string ClientEmail,
    string? Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
