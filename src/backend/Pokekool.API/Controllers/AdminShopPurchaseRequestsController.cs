using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Shop.Commands.UpdatePurchaseRequestStatus;
using Pokekool.Application.Shop.Queries.GetPurchaseRequests;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

/// <summary>Gestion admin des demandes d'achat (PurchaseRequest).</summary>
[Route("api/v{version:apiVersion}/admin/shop/purchase-requests")]
[Authorize(Roles = "admin")]
public sealed class AdminShopPurchaseRequestsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminShopPurchaseRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseRequestDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List purchase requests (filter by status)")]
    public async Task<ActionResult<PagedResult<PurchaseRequestDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetPurchaseRequestsQuery(status, page, pageSize), ct));

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Change a purchase request status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdatePurchaseRequestStatusBody body,
        CancellationToken ct)
    {
        await _mediator.Send(new UpdatePurchaseRequestStatusCommand(id, body.Status), ct);
        return NoContent();
    }
}

public sealed record UpdatePurchaseRequestStatusBody(string Status);
