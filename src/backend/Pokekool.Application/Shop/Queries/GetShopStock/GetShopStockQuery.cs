using MediatR;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopStock;

public sealed record GetShopStockQuery(
    string? Type = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<ShopStockDto>>;
