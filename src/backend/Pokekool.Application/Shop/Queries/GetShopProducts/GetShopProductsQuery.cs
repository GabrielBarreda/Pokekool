using MediatR;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopProducts;

public sealed record GetShopProductsQuery(
    string? Type = null,
    bool? IsAvailable = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<ShopProductDto>>;
