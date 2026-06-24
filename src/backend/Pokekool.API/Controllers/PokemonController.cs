using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Pokemon.Commands.SyncPokemonTcg;
using Pokekool.Application.Pokemon.Queries.GetCardById;
using Pokekool.Application.Pokemon.Queries.GetCards;
using Pokekool.Application.Pokemon.Queries.GetSeries;
using Pokekool.Application.Pokemon.Queries.GetSetById;
using Pokekool.Application.Pokemon.Queries.GetSets;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;
using Pokekool.Shared.Requests.Pokemon;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[Route("api/v{version:apiVersion}/pokemon")]
public sealed class PokemonController : BaseApiController
{
    private readonly IMediator _mediator;

    public PokemonController(IMediator mediator) => _mediator = mediator;

    [HttpGet("series")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PokemonSeriesDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List all Pokemon series")]
    public async Task<ActionResult<IReadOnlyList<PokemonSeriesDto>>> GetSeries(CancellationToken ct)
        => Ok(await _mediator.Send(new GetSeriesQuery(), ct));

    [HttpGet("sets")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PokemonSetDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List all Pokemon sets", Description = "Filter by seriesId (optional).")]
    public async Task<ActionResult<IReadOnlyList<PokemonSetDto>>> GetSets(
        [FromQuery] Guid? seriesId,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetSetsQuery(seriesId), ct));

    [HttpGet("sets/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PokemonSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Get a Pokemon set by ID")]
    public async Task<ActionResult<PokemonSetDto>> GetSetById(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSetByIdQuery(id), ct));

    [HttpGet("cards")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<PokemonCardSummaryDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Search Pokemon cards", Description = "Filterable by search, setId, rarity, supertype. Paginated.")]
    public async Task<ActionResult<PagedResult<PokemonCardSummaryDto>>> GetCards(
        [FromQuery] string? search,
        [FromQuery] Guid? setId,
        [FromQuery] string? rarity,
        [FromQuery] string? supertype,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCardsQuery(search, setId, rarity, supertype, page, pageSize), ct));

    [HttpGet("cards/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PokemonCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Get a Pokemon card by ID")]
    public async Task<ActionResult<PokemonCardDto>> GetCardById(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCardByIdQuery(id), ct));

    [HttpPost("sync")]
    [RequirePermission(Permissions.CardsSync)]
    [ProducesResponseType(typeof(SyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(Summary = "Sync with PokemonTCG API", Description = "Imports / updates series, sets and cards from the external PokemonTCG API. Requires cards:sync permission.")]
    public async Task<ActionResult<SyncResultDto>> SyncPokemonTcg(
        [FromBody] SyncPokemonTcgRequest request,
        CancellationToken ct)
        => Ok(await _mediator.Send(new SyncPokemonTcgCommand(request.ForceRefresh), ct));
}
