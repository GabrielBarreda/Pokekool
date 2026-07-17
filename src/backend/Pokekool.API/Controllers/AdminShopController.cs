using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Shop.Commands.CreateShopProduct;
using Pokekool.Application.Shop.Commands.CreateShopStock;
using Pokekool.Application.Shop.Commands.DeleteShopProduct;
using Pokekool.Application.Shop.Commands.DeleteShopStock;
using Pokekool.Application.Shop.Commands.ToggleShopProductAvailability;
using Pokekool.Application.Shop.Commands.UpdateAppointment;
using Pokekool.Application.Shop.Commands.UpdateAppointmentStatus;
using Pokekool.Application.Shop.Commands.UpdateShopInfo;
using Pokekool.Application.Shop.Commands.UpdateShopProduct;
using Pokekool.Application.Shop.Commands.UpdateShopStock;
using Pokekool.Application.Shop.Commands.UploadShopPlvModel;
using Pokekool.Application.Shop.Queries.GetShopAppointmentById;
using Pokekool.Application.Shop.Queries.GetShopPlvModel;
using Pokekool.Application.Shop.Queries.GetShopAppointments;
using Pokekool.Application.Shop.Queries.GetShopInfo;
using Pokekool.Application.Shop.Queries.GetProductInterest;
using Pokekool.Application.Shop.Queries.GetShopProducts;
using Pokekool.Application.Shop.Queries.GetShopStock;
using Pokekool.Application.Storage.Queries;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Shop;
using Pokekool.Shared.Dtos.Storage;
using Pokekool.Shared.Requests.Shop;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

/// <summary>
/// Module Boutique — endpoints Admin / Employee.
/// Infos boutique : réservées Admin (shop:manage). Présentoir / Stock / Planning : shop:read + shop:write.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/shop")]
[Produces("application/json")]
[Authorize]
public sealed class AdminShopController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminShopController(IMediator mediator) => _mediator = mediator;

    // ============================================================
    // PAGE 1 — INFOS BOUTIQUE (Admin only)
    // ============================================================

    [HttpGet("info")]
    [RequirePermission(Permissions.ShopManage)]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Get shop info (singleton, created empty if absent)")]
    public async Task<ActionResult<ShopDto>> GetInfo(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShopInfoQuery(), ct));

    [HttpPut("info")]
    [RequirePermission(Permissions.ShopManage)]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Update shop info")]
    public async Task<ActionResult<ShopDto>> UpdateInfo(
        [FromBody] UpdateShopInfoRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UpdateShopInfoCommand(
            body.Name, body.Description, body.Address, body.City, body.ZipCode,
            body.GoogleMapsUrl, body.HoursJson, body.Services,
            body.InstagramUrl, body.TiktokUrl, body.PhotoUrls), ct));

    // ============================================================
    // PLV PRÉSENTOIR — modèle 3D (.blend → .glb, pipeline Blender partagé)
    // ============================================================

    [HttpGet("plv/model")]
    [RequirePermission(Permissions.ShopRead)]
    [ProducesResponseType(typeof(ShopPlvModelDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "URL du modèle 3D de la PLV (présentoir) — lecture pour le viewer")]
    public async Task<ActionResult<ShopPlvModelDto>> GetPlvModel(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShopPlvModelQuery(), ct));

    [HttpPost("plv/model")]
    [RequirePermission(Permissions.ShopWrite)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(200_000_000)] // .blend potentiellement volumineux
    [ProducesResponseType(typeof(StorageCompileJobDto), StatusCodes.Status202Accepted)]
    [SwaggerOperation(Summary = "Uploader le projet Blender (.blend) de la PLV — compilation .glb asynchrone")]
    public async Task<ActionResult<StorageCompileJobDto>> UploadPlvModel(
        IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Fichier .blend manquant ou vide." });

        await using var stream = file.OpenReadStream();
        var job = await _mediator.Send(new UploadShopPlvModelCommand(stream, file.FileName), ct);
        return Accepted(job);
    }

    [HttpGet("plv/compile-jobs/{jobId:guid}")]
    [RequirePermission(Permissions.ShopRead)]
    [ProducesResponseType(typeof(StorageCompileJobDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Statut d'un job de compilation Blender de la PLV (polling)")]
    public async Task<ActionResult<StorageCompileJobDto>> GetPlvCompileJob(
        Guid jobId, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCompileJobStatusQuery(jobId), ct));

    // ============================================================
    // PAGE 2 — PRÉSENTOIR (Admin + Employee)
    // ============================================================

    [HttpGet("products")]
    [RequirePermission(Permissions.ShopRead)]
    [ProducesResponseType(typeof(PagedResult<ShopProductDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List shop products (paged + filters)", OperationId = "AdminShop_GetProducts")]
    public async Task<ActionResult<PagedResult<ShopProductDto>>> GetProducts(
        [FromQuery] string? type,
        [FromQuery] bool? isAvailable,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShopProductsQuery(type, isAvailable, search, page, pageSize), ct));

    [HttpGet("products/{id:guid}/interest")]
    [RequirePermission(Permissions.ShopRead)]
    [ProducesResponseType(typeof(ProductInterestDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Nombre de clients intéressés (demandes d'achat actives) par un article")]
    public async Task<ActionResult<ProductInterestDto>> GetProductInterest(Guid id, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProductInterestQuery(id), ct));

    [HttpPost("products")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(typeof(ShopProductDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Create a shop product")]
    public async Task<ActionResult<ShopProductDto>> CreateProduct(
        [FromBody] ShopProductRequest body, CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new CreateShopProductCommand(
            body.Type, body.CardId, body.ProductName, body.Language ?? "EN", body.Condition, body.GradingCompany,
            body.GradeValue, body.Price, body.Currency, body.Quantity, body.IsAvailable,
            body.Description, body.PhotoUrl), ct);
        return Created($"/api/v1/admin/shop/products/{dto.Id}", dto);
    }

    [HttpPut("products/{id:guid}")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(typeof(ShopProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Update a shop product")]
    public async Task<ActionResult<ShopProductDto>> UpdateProduct(
        Guid id, [FromBody] ShopProductRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UpdateShopProductCommand(
            id, body.Type, body.CardId, body.ProductName, body.Language ?? "EN", body.Condition, body.GradingCompany,
            body.GradeValue, body.Price, body.Currency, body.Quantity, body.IsAvailable,
            body.Description, body.PhotoUrl), ct));

    [HttpDelete("products/{id:guid}")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Delete a shop product")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteShopProductCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("products/{id:guid}/availability")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(typeof(ShopProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Toggle product availability (forced false if quantity = 0)")]
    public async Task<ActionResult<ShopProductDto>> ToggleAvailability(
        Guid id, [FromBody] ToggleAvailabilityRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new ToggleShopProductAvailabilityCommand(id, body.IsAvailable), ct));

    // ============================================================
    // PAGE 3 — STOCK (Admin + Employee)
    // ============================================================

    [HttpGet("stock")]
    [RequirePermission(Permissions.ShopRead)]
    [ProducesResponseType(typeof(PagedResult<ShopStockDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List shop stock (paged + filters)")]
    public async Task<ActionResult<PagedResult<ShopStockDto>>> GetStock(
        [FromQuery] string? type,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShopStockQuery(type, search, page, pageSize), ct));

    [HttpPost("stock")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(typeof(ShopStockDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Create a stock entry")]
    public async Task<ActionResult<ShopStockDto>> CreateStock(
        [FromBody] ShopStockRequest body, CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new CreateShopStockCommand(
            body.CardId, body.ProductName, body.Type, body.Language ?? "EN", body.Condition, body.GradingCompany,
            body.GradeValue, body.QuantityTotal, body.QuantityAvailable, body.QuantityReserved,
            body.PurchasePrice, body.PurchaseCurrency, body.Notes), ct);
        return Created($"/api/v1/admin/shop/stock/{dto.Id}", dto);
    }

    [HttpPut("stock/{id:guid}")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(typeof(ShopStockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Update a stock entry")]
    public async Task<ActionResult<ShopStockDto>> UpdateStock(
        Guid id, [FromBody] ShopStockRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UpdateShopStockCommand(
            id, body.CardId, body.ProductName, body.Type, body.Language ?? "EN", body.Condition, body.GradingCompany,
            body.GradeValue, body.QuantityTotal, body.QuantityAvailable, body.QuantityReserved,
            body.PurchasePrice, body.PurchaseCurrency, body.Notes), ct));

    [HttpDelete("stock/{id:guid}")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Delete a stock entry")]
    public async Task<IActionResult> DeleteStock(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteShopStockCommand(id), ct);
        return NoContent();
    }

    // ============================================================
    // PAGE 4 — SERVICES & PLANNING RDV (Admin + Employee)
    // ============================================================

    [HttpGet("appointments")]
    [RequirePermission(Permissions.ShopRead)]
    [ProducesResponseType(typeof(PagedResult<ShopAppointmentDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List appointments (filters: status, service, date range)")]
    public async Task<ActionResult<PagedResult<ShopAppointmentDto>>> GetAppointments(
        [FromQuery] string? status,
        [FromQuery] string? serviceType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShopAppointmentsQuery(status, serviceType, from, to, page, pageSize), ct));

    [HttpGet("appointments/{id:guid}")]
    [RequirePermission(Permissions.ShopRead)]
    [ProducesResponseType(typeof(ShopAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Get an appointment by ID")]
    public async Task<ActionResult<ShopAppointmentDto>> GetAppointmentById(Guid id, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShopAppointmentByIdQuery(id), ct));

    [HttpPut("appointments/{id:guid}/status")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(typeof(ShopAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Change appointment status")]
    public async Task<ActionResult<ShopAppointmentDto>> UpdateAppointmentStatus(
        Guid id, [FromBody] UpdateAppointmentStatusRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UpdateAppointmentStatusCommand(id, body.Status), ct));

    [HttpPut("appointments/{id:guid}")]
    [RequirePermission(Permissions.ShopWrite)]
    [ProducesResponseType(typeof(ShopAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Update appointment slot / internal notes")]
    public async Task<ActionResult<ShopAppointmentDto>> UpdateAppointment(
        Guid id, [FromBody] UpdateAppointmentRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UpdateAppointmentCommand(
            id, body.ScheduledAt, body.DurationMinutes, body.ServiceType,
            body.NotesInternal, body.AssignedToUserId), ct));
}
