using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Storage.Commands;
using Pokekool.Application.Storage.Queries;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Storage;
using Pokekool.Shared.Requests.Storage;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

/// <summary>
/// Module Stockage Physique — endpoints Admin / Employee.
/// Salles, cartons, classeurs, localisations, viewer 3D (upload .blend + compilation Blender)
/// et confirmation des déplacements détectés.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/storage")]
[Produces("application/json")]
[Authorize]
public sealed class AdminStorageController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminStorageController(IMediator mediator) => _mediator = mediator;

    // ============================================================
    // SALLES
    // ============================================================

    [HttpGet("rooms")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(IReadOnlyList<StorageRoomDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Liste des salles de stockage de la boutique")]
    public async Task<ActionResult<IReadOnlyList<StorageRoomDto>>> GetRooms(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStorageRoomsQuery(), ct));

    [HttpPost("rooms")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageRoomDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Créer une salle de stockage")]
    public async Task<ActionResult<StorageRoomDto>> CreateRoom(
        [FromBody] CreateStorageRoomRequest body, CancellationToken ct = default)
    {
        var room = await _mediator.Send(new CreateStorageRoomCommand(body.Name), ct);
        return CreatedAtAction(nameof(GetRoomLayout), new { id = room.Id, version = "1.0" }, room);
    }

    [HttpGet("rooms/{id:guid}/layout")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(StorageRoomLayoutDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Salle + cartons + classeurs (chargement de la vue 3D)")]
    public async Task<ActionResult<StorageRoomLayoutDto>> GetRoomLayout(
        Guid id, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetRoomLayoutQuery(id), ct));

    // ============================================================
    // MODÈLE 3D — upload .blend + compilation Blender asynchrone
    // ============================================================

    [HttpPost("rooms/{id:guid}/model")]
    [RequirePermission(Permissions.StorageWrite)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(200_000_000)] // .blend potentiellement volumineux
    [ProducesResponseType(typeof(StorageCompileJobDto), StatusCodes.Status202Accepted)]
    [SwaggerOperation(Summary = "Uploader un projet Blender (.blend) — compilation .glb asynchrone")]
    public async Task<ActionResult<StorageCompileJobDto>> UploadRoomModel(
        Guid id, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Fichier .blend manquant ou vide." });

        await using var stream = file.OpenReadStream();
        var job = await _mediator.Send(new UploadRoomModelCommand(id, stream, file.FileName), ct);
        return Accepted(job);
    }

    [HttpGet("compile-jobs/{jobId:guid}")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(StorageCompileJobDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Statut d'un job de compilation Blender (polling)")]
    public async Task<ActionResult<StorageCompileJobDto>> GetCompileJob(
        Guid jobId, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCompileJobStatusQuery(jobId), ct));

    // ============================================================
    // CARTONS
    // ============================================================

    [HttpGet("boxes")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(IReadOnlyList<StorageBoxDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Liste des cartons d'une salle (filtre label_origin optionnel)")]
    public async Task<ActionResult<IReadOnlyList<StorageBoxDto>>> GetBoxes(
        [FromQuery] Guid roomId,
        [FromQuery] string? labelOrigin,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStorageBoxesQuery(roomId, labelOrigin), ct));

    [HttpPost("boxes")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageBoxDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Créer un carton (barcode XOR internal_code selon l'origine)")]
    public async Task<ActionResult<StorageBoxDto>> CreateBox(
        [FromBody] CreateStorageBoxRequest body, CancellationToken ct = default)
    {
        var box = await _mediator.Send(new CreateStorageBoxCommand(
            body.RoomId, body.LabelOrigin, body.Barcode, body.InternalCode,
            body.SupplierName, body.Size, body.Position3dJson, body.Notes), ct);
        return StatusCode(StatusCodes.Status201Created, box);
    }

    [HttpPatch("boxes/{id:guid}/position")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageBoxDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Déplacer un carton dans la vue 3D (position_3d)")]
    public async Task<ActionResult<StorageBoxDto>> UpdateBoxPosition(
        Guid id, [FromBody] UpdateBoxPositionRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UpdateStorageBoxPositionCommand(id, body.Position3dJson), ct));

    // ============================================================
    // CLASSEURS
    // ============================================================

    [HttpGet("binders")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(IReadOnlyList<StorageBinderDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Liste des classeurs d'un carton")]
    public async Task<ActionResult<IReadOnlyList<StorageBinderDto>>> GetBinders(
        [FromQuery] Guid boxId, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStorageBindersQuery(boxId), ct));

    [HttpPost("binders")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageBinderDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Créer un classeur (dans un carton OU sur un meuble)")]
    public async Task<ActionResult<StorageBinderDto>> CreateBinder(
        [FromBody] CreateStorageBinderRequest body, CancellationToken ct = default)
    {
        var binder = await _mediator.Send(new CreateStorageBinderCommand(
            body.Label, body.BoxId, body.PositionInBox, body.FurnitureId, body.LevelIndex), ct);
        return StatusCode(StatusCodes.Status201Created, binder);
    }

    [HttpPatch("binders/{id:guid}/placement")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageBinderDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Ranger un classeur dans un carton ou sur un meuble")]
    public async Task<ActionResult<StorageBinderDto>> PlaceBinder(
        Guid id, [FromBody] PlaceBinderRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new PlaceBinderCommand(
            id, body.BoxId, body.PositionInBox, body.FurnitureId, body.LevelIndex), ct));

    // ============================================================
    // MEUBLES (étagères)
    // ============================================================

    [HttpPost("furniture")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageFurnitureDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Créer un meuble (choix du modèle : ShelfClassic6 | ShelfWall)")]
    public async Task<ActionResult<StorageFurnitureDto>> CreateFurniture(
        [FromBody] CreateStorageFurnitureRequest body, CancellationToken ct = default)
    {
        var f = await _mediator.Send(new CreateStorageFurnitureCommand(body.RoomId, body.Type, body.Name), ct);
        return StatusCode(StatusCodes.Status201Created, f);
    }

    [HttpPatch("furniture/{id:guid}/position")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageFurnitureDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Déplacer un meuble dans la vue 3D")]
    public async Task<ActionResult<StorageFurnitureDto>> UpdateFurniturePosition(
        Guid id, [FromBody] UpdateFurniturePositionRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UpdateFurniturePositionCommand(id, body.Position3dJson), ct));

    [HttpDelete("furniture/{id:guid}")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Supprimer un meuble (son contenu est détaché, pas supprimé)")]
    public async Task<IActionResult> DeleteFurniture(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteStorageFurnitureCommand(id), ct);
        return NoContent();
    }

    // ============================================================
    // RANGEMENT DES CARTONS
    // ============================================================

    [HttpPatch("boxes/{id:guid}/placement")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageBoxDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Ranger un carton sur un meuble/niveau ou au sol")]
    public async Task<ActionResult<StorageBoxDto>> PlaceBox(
        Guid id, [FromBody] PlaceBoxRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new PlaceBoxCommand(
            id, body.FurnitureId, body.LevelIndex, body.Position3dJson), ct));

    // ============================================================
    // LOCALISATIONS
    // ============================================================

    [HttpPost("locations")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageLocationDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Rattacher un item (stock / collection) à un classeur")]
    public async Task<ActionResult<StorageLocationDto>> AssignLocation(
        [FromBody] AssignStorageLocationRequest body, CancellationToken ct = default)
    {
        var location = await _mediator.Send(new AssignStorageLocationCommand(
            body.BinderId, body.OwnerType, body.OwnerId, body.SlotPosition), ct);
        return StatusCode(StatusCodes.Status201Created, location);
    }

    // ============================================================
    // LOOKUP (scan théorique par code)
    // ============================================================

    [HttpGet("lookup")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(StorageBoxLookupDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Rechercher un carton par code-barre OU code interne")]
    public async Task<ActionResult<StorageBoxLookupDto>> Lookup(
        [FromQuery] string code, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBoxContentLookupQuery(code), ct));

    // ============================================================
    // DÉPLACEMENTS (confirmation humaine)
    // ============================================================

    [HttpGet("movements/pending")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(IReadOnlyList<StorageMovementEventDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Événements de déplacement en attente de confirmation")]
    public async Task<ActionResult<IReadOnlyList<StorageMovementEventDto>>> GetPendingMovements(
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetPendingMovementsQuery(), ct));

    [HttpPost("movements/{id:guid}/confirm")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageMovementEventDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Confirmer ou rejeter un déplacement détecté")]
    public async Task<ActionResult<StorageMovementEventDto>> ConfirmMovement(
        Guid id, [FromBody] ConfirmMovementRequest body, CancellationToken ct = default)
        => Ok(await _mediator.Send(new CreateMovementConfirmationCommand(id, body.Confirm), ct));
}
