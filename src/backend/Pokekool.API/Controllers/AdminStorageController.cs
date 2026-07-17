using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Collection.Commands.ScanCollection;
using Pokekool.Application.Storage.Commands;
using Pokekool.Application.Storage.Queries;
using Pokekool.Application.Storage.Settings;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Collection;
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

    [HttpDelete("rooms/{id:guid}")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Supprimer une salle (et tout son contenu : meubles, cartons, classeurs)")]
    public async Task<IActionResult> DeleteRoom(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteStorageRoomCommand(id), ct);
        return NoContent();
    }

    [HttpGet("rooms/{id:guid}/layout")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(StorageRoomLayoutDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Salle + cartons + classeurs (chargement de la vue 3D)")]
    public async Task<ActionResult<StorageRoomLayoutDto>> GetRoomLayout(
        Guid id, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetRoomLayoutQuery(id), ct));

    // ============================================================
    // PARAMÈTRES DU MODULE (onglet Paramètres › Stockage)
    // ============================================================

    [HttpGet("settings")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(StorageSettingsDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Paramètres du module Stockage (ex. suppression en chaîne)")]
    public async Task<ActionResult<StorageSettingsDto>> GetSettings(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStorageSettingsQuery(), ct));

    [HttpPut("settings")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(StorageSettingsDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Mettre à jour les paramètres du module Stockage")]
    public async Task<ActionResult<StorageSettingsDto>> UpdateSettings(
        [FromBody] UpdateStorageSettingsRequest body, CancellationToken ct = default)
    {
        await _mediator.Send(new UpdateStorageSettingsCommand(body.CascadeDelete), ct);
        return Ok(await _mediator.Send(new GetStorageSettingsQuery(), ct));
    }

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

    [HttpDelete("boxes/{id:guid}")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Supprimer un carton (et par cascade ses classeurs et leurs localisations)")]
    public async Task<IActionResult> DeleteBox(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteStorageBoxCommand(id), ct);
        return NoContent();
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

    [HttpGet("estimate")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(StorageEstimateDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Valeur estimée d'un carton/classeur (somme des cotes des cartes rangées)")]
    public async Task<ActionResult<StorageEstimateDto>> GetEstimate(
        [FromQuery] string ownerType, [FromQuery] Guid ownerId, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStorageEstimateQuery(ownerType, ownerId), ct));

    [HttpGet("card-detail")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(StorageCardDetailDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Détail comptoir d'une carte en stock (grade, prix estimé, valeurs récentes)")]
    public async Task<ActionResult<StorageCardDetailDto>> GetCardDetail(
        [FromQuery] string ownerType, [FromQuery] Guid ownerId, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStockCardDetailQuery(ownerType, ownerId), ct));

    [HttpGet("binders/{id:guid}/cards")]
    [RequirePermission(Permissions.StorageRead)]
    [ProducesResponseType(typeof(IReadOnlyList<StorageBinderCardDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Cartes rangées dans un classeur (pour la vue classeur)")]
    public async Task<ActionResult<IReadOnlyList<StorageBinderCardDto>>> GetBinderCards(
        Guid id, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBinderCardsQuery(id), ct));

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

    [HttpPost("binders/{id:guid}/assign")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(IReadOnlyList<StorageLocationDto>), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Assigner en lot des articles du stock boutique à un classeur")]
    public async Task<ActionResult<IReadOnlyList<StorageLocationDto>>> BatchAssign(
        Guid id, [FromBody] BatchAssignBinderRequest body, CancellationToken ct = default)
    {
        var locations = await _mediator.Send(
            new BatchAssignStorageLocationCommand(id, "shop_stock", body.OwnerIds), ct);
        return StatusCode(StatusCodes.Status201Created, locations);
    }

    // ============================================================
    // SCAN CLASSEUR DOUBLE PAGE (réutilise pipeline vision collection)
    // ============================================================

    [HttpPost("scan-binder")]
    [RequirePermission(Permissions.StorageWrite)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    [ProducesResponseType(typeof(ScanCollectionResult), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Scanner une double page de classeur via vision LLM (réutilise pipeline scan collection)")]
    public async Task<ActionResult<ScanCollectionResult>> ScanBinder(
        IFormFile image, CancellationToken ct = default)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new { message = "Image manquante ou vide." });

        await using var stream = image.OpenReadStream();
        var buffer = new byte[stream.Length];
        _ = await stream.ReadAsync(buffer, ct);
        var base64 = Convert.ToBase64String(buffer);

        var result = await _mediator.Send(new ScanCollectionCommand(base64), ct);
        return Ok(result);
    }

    [HttpPost("create-from-scan")]
    [RequirePermission(Permissions.StorageWrite)]
    [ProducesResponseType(typeof(IReadOnlyList<StorageLocationDto>), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Créer les ShopStock + StorageLocation depuis un scan classeur validé")]
    public async Task<ActionResult<IReadOnlyList<StorageLocationDto>>> CreateFromScan(
        [FromBody] CreateFromScanRequest body, CancellationToken ct = default)
    {
        var locations = await _mediator.Send(
            new CreateBinderFromScanCommand(body.BinderId, body.ValidatedOwnerIds), ct);
        return StatusCode(StatusCodes.Status201Created, locations);
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
