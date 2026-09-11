using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Collection.Commands.BulkAddToCollection;
using Pokekool.Application.Collection.Commands.ScanCollection;
using Pokekool.Application.Collection.Commands.SetCardFavorite;
using Pokekool.Application.Collection.Commands.SetCardPriority;
using Pokekool.Application.Collection.Commands.ToggleCardOwned;
using Pokekool.Application.Collection.Queries.GetCollectionCards;
using Pokekool.Application.Collection.Queries.GetCollectionStats;
using Pokekool.Application.Collection.Queries.GetCollectionValue;
using Pokekool.Shared.Dtos.Collection;
using Pokekool.Shared.Dtos.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[Route("api/v{version:apiVersion}/collection")]
[Authorize]
public sealed class CollectionController : BaseApiController
{
    private readonly IMediator _mediator;

    public CollectionController(IMediator mediator) => _mediator = mediator;

    [HttpGet("cards")]
    [ProducesResponseType(typeof(PagedResult<CollectionCardDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Get collection cards", Description = "Paged list of cards with collection status for the current user.")]
    public async Task<ActionResult<PagedResult<CollectionCardDto>>> GetCards(
        [FromQuery] Guid? setId,
        [FromQuery] string? search,
        [FromQuery] string? rarity,
        [FromQuery] int? year,
        [FromQuery] bool? ownedOnly,
        [FromQuery] bool? favoritesOnly,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 60,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(
            new GetCollectionCardsQuery(setId, search, rarity, year, ownedOnly, favoritesOnly, sortBy, page, pageSize), ct));

    [HttpGet("stats")]
    [ProducesResponseType(typeof(CollectionStatsDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Get collection stats for the current user")]
    public async Task<ActionResult<CollectionStatsDto>> GetStats(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCollectionStatsQuery(), ct));

    [HttpGet("value")]
    [ProducesResponseType(typeof(CollectionValueDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Get estimated total value of the current user's owned cards")]
    public async Task<ActionResult<CollectionValueDto>> GetValue(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCollectionValueQuery(), ct));

    [HttpPost("cards/{cardId:guid}/toggle-owned")]
    [ProducesResponseType(typeof(CollectionCardStatusDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Toggle owned status of a card")]
    public async Task<ActionResult<CollectionCardStatusDto>> ToggleOwned(Guid cardId, CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleCardOwnedCommand(cardId), ct));

    [HttpPost("cards/{cardId:guid}/favorite")]
    [ProducesResponseType(typeof(CollectionCardStatusDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Set favorite status of a card")]
    public async Task<ActionResult<CollectionCardStatusDto>> SetFavorite(
        Guid cardId,
        [FromBody] SetFavoriteRequest body,
        CancellationToken ct)
        => Ok(await _mediator.Send(new SetCardFavoriteCommand(cardId, body.IsFavorite), ct));

    [HttpPost("cards/{cardId:guid}/priority")]
    [ProducesResponseType(typeof(CollectionCardStatusDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Set priority status of a card (only for unowned cards)")]
    public async Task<ActionResult<CollectionCardStatusDto>> SetPriority(
        Guid cardId,
        [FromBody] SetPriorityRequest body,
        CancellationToken ct)
        => Ok(await _mediator.Send(new SetCardPriorityCommand(cardId, body.IsPriority), ct));

    [HttpPost("scan")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ScanCollectionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Scan a photo and detect Pokémon cards via Vision LLM")]
    public async Task<ActionResult<ScanCollectionResult>> ScanImage(
        IFormFile image,
        [FromForm] List<string>? excluded,
        [FromForm] List<Guid>? excludedCardIds,
        CancellationToken ct)
    {
        if (image is null || image.Length == 0)
            return BadRequest("Image requise.");
        if (image.Length > 10 * 1024 * 1024)
            return BadRequest("Image trop grande (max 10 Mo).");

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());

        return Ok(await _mediator.Send(new ScanCollectionCommand(base64, excluded, excludedCardIds), ct));
    }

    [HttpPost("bulk-own")]
    [ProducesResponseType(typeof(BulkAddResult), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Add multiple scanned cards to the current user's collection")]
    public async Task<ActionResult<BulkAddResult>> BulkAddOwned(
        [FromBody] BulkAddOwnedRequest body,
        CancellationToken ct)
        => Ok(await _mediator.Send(
            new BulkAddToCollectionCommand(body.CardIds, body.Matches), ct));
}

public sealed record SetFavoriteRequest(bool IsFavorite);
public sealed record SetPriorityRequest(bool IsPriority);

public sealed record BulkAddOwnedRequest(
    IReadOnlyList<Guid> CardIds,
    IReadOnlyList<CardScanMatchDto> Matches);
