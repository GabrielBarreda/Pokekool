using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Collection.Commands.SetCardFavorite;
using Pokekool.Application.Collection.Commands.SetCardPriority;
using Pokekool.Application.Collection.Commands.ToggleCardOwned;
using Pokekool.Application.Collection.Queries.GetCollectionCards;
using Pokekool.Application.Collection.Queries.GetCollectionStats;
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
}

public sealed record SetFavoriteRequest(bool IsFavorite);
public sealed record SetPriorityRequest(bool IsPriority);
