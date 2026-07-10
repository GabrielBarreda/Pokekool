using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Shop.Commands.CancelMyAppointment;
using Pokekool.Application.Shop.Commands.CreateClientAppointment;
using Pokekool.Application.Shop.Commands.RequestPurchase;
using Pokekool.Application.Shop.Queries.GetAvailableSlots;
using Pokekool.Application.Shop.Queries.GetClientShopProducts;
using Pokekool.Application.Shop.Queries.GetMyAppointments;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

/// <summary>
/// Endpoints boutique côté client authentifié (rôle client ou admin).
/// Aucune transaction financière : demandes d'achat + prises de RDV en main propre.
/// </summary>
[Route("api/v{version:apiVersion}/shop")]
[Authorize(Roles = "client,admin")]
public sealed class ShopClientController : BaseApiController
{
    private readonly IMediator _mediator;

    public ShopClientController(IMediator mediator) => _mediator = mediator;

    // ── Catalogue ──────────────────────────────────────────────
    [HttpGet("products")]
    [ProducesResponseType(typeof(PagedResult<ShopProductDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List available shop products")]
    public async Task<ActionResult<PagedResult<ShopProductDto>>> GetProducts(
        [FromQuery] string? type,
        [FromQuery(Name = "grade_min")] decimal? gradeMin,
        [FromQuery(Name = "grade_max")] decimal? gradeMax,
        [FromQuery(Name = "price_max")] decimal? priceMax,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(
            new GetClientShopProductsQuery(type, gradeMin, gradeMax, priceMax, search, page, pageSize), ct));

    [HttpPost("products/{id:guid}/request-purchase")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Create a purchase request for a product")]
    public async Task<ActionResult<Guid>> RequestPurchase(
        Guid id,
        [FromBody] RequestPurchaseBody body,
        CancellationToken ct)
        => Ok(await _mediator.Send(new RequestPurchaseCommand(id, body?.Message), ct));

    // ── RDV ────────────────────────────────────────────────────
    [HttpGet("appointments/available-slots")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlotDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Available appointment slots for a service on a date")]
    public async Task<ActionResult<IReadOnlyList<AvailableSlotDto>>> GetAvailableSlots(
        [FromQuery(Name = "service_type")] string serviceType,
        [FromQuery] string date,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetAvailableSlotsQuery(serviceType, date), ct));

    [HttpPost("appointments")]
    [ProducesResponseType(typeof(ShopAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Book an appointment (UserId from JWT, name/email from account)")]
    public async Task<ActionResult<ShopAppointmentDto>> CreateAppointment(
        [FromBody] CreateAppointmentBody body,
        CancellationToken ct)
        => Ok(await _mediator.Send(
            new CreateClientAppointmentCommand(body.ServiceType, body.ScheduledAt, body.NotesClient), ct));

    [HttpGet("appointments/mine")]
    [ProducesResponseType(typeof(PagedResult<ShopAppointmentDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "My appointments (filtered by JWT account)")]
    public async Task<ActionResult<PagedResult<ShopAppointmentDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetMyAppointmentsQuery(page, pageSize), ct));

    [HttpDelete("appointments/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Cancel my appointment (must be pending and >24h ahead)")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelMyAppointmentCommand(id), ct);
        return NoContent();
    }
}

public sealed record RequestPurchaseBody(string? Message);
public sealed record CreateAppointmentBody(string ServiceType, DateTimeOffset ScheduledAt, string? NotesClient);
