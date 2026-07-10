using MediatR;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetPurchaseRequests;

/// <summary>Liste admin des demandes d'achat, filtrable par statut.</summary>
public sealed record GetPurchaseRequestsQuery(string? Status, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<PurchaseRequestDto>>;

public sealed class GetPurchaseRequestsQueryHandler
    : IRequestHandler<GetPurchaseRequestsQuery, PagedResult<PurchaseRequestDto>>
{
    private readonly IPurchaseRequestRepository _repo;

    public GetPurchaseRequestsQueryHandler(IPurchaseRequestRepository repo) => _repo = repo;

    public async Task<PagedResult<PurchaseRequestDto>> Handle(GetPurchaseRequestsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        PurchaseRequestStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<PurchaseRequestStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var (items, total) = await _repo.GetPagedAsync(statusFilter, page, pageSize, cancellationToken);

        var dtos = items.Select(p => new PurchaseRequestDto(
            p.Id, p.ShopProductId, p.ProductName, p.ProductPrice, p.ProductCurrency,
            p.UserId, p.ClientName, p.ClientEmail, p.Message, p.Status, p.CreatedAt, p.UpdatedAt)).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<PurchaseRequestDto>(dtos, page, pageSize, total, totalPages);
    }
}
