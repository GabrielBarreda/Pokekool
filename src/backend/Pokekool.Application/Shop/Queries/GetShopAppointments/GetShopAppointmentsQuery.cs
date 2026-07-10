using MediatR;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopAppointments;

public sealed record GetShopAppointmentsQuery(
    string? Status = null,
    string? ServiceType = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 100) : IRequest<PagedResult<ShopAppointmentDto>>;
