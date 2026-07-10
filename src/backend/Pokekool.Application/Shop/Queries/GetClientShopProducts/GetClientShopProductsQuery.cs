using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetClientShopProducts;

/// <summary>Catalogue boutique visible par le client : seulement les produits disponibles.</summary>
public sealed record GetClientShopProductsQuery(
    string? Type,
    decimal? GradeMin,
    decimal? GradeMax,
    decimal? PriceMax,
    string? Search,
    int Page = 1,
    int PageSize = 24) : IRequest<PagedResult<ShopProductDto>>;

public sealed class GetClientShopProductsQueryHandler
    : IRequestHandler<GetClientShopProductsQuery, PagedResult<ShopProductDto>>
{
    private readonly IShopClientRepository _repo;

    public GetClientShopProductsQueryHandler(IShopClientRepository repo) => _repo = repo;

    public async Task<PagedResult<ShopProductDto>> Handle(GetClientShopProductsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 24 : request.PageSize;

        var (items, total) = await _repo.GetAvailableProductsPagedAsync(
            request.Type, request.GradeMin, request.GradeMax, request.PriceMax, request.Search,
            page, pageSize, cancellationToken);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<ShopProductDto>(items, page, pageSize, total, totalPages);
    }
}
