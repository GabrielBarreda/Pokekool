using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Pokemon.Queries.GetMarketCards;
using Pokekool.Application.Pokemon.Queries.GetSetFilters;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[Route("api/v{version:apiVersion}/market")]
public sealed class MarketController : BaseApiController
{
    private readonly IMediator _mediator;

    public MarketController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Retourne les rarités et supertypes disponibles pour un set.
    /// </summary>
    [HttpGet("sets/{setId:guid}/filters")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SetFiltersDto), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Filtres disponibles pour un set",
        Description = "Rarités et supertypes distincts présents dans le set, pour griser les filtres indisponibles côté client.")]
    public async Task<ActionResult<SetFiltersDto>> GetSetFilters(
        Guid setId,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSetFiltersQuery(setId), ct));

    /// <summary>
    /// Liste les cartes d'un set avec leurs prix de marché résumés (bourse).
    /// </summary>
    [HttpGet("sets/{setId:guid}/cards")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<MarketCardDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Liste des cartes du marché pour un set",
        Description = "Retourne les cartes d'un set enrichies du meilleur prix marché disponible (tcgplayer holofoil > normal > cardmarket).")]
    public async Task<ActionResult<PagedResult<MarketCardDto>>> GetMarketCards(
        Guid setId,
        [FromQuery] string? search,
        [FromQuery] string? rarity,
        [FromQuery] string? supertype,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 60,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(
            new GetMarketCardsQuery(setId, search, rarity, supertype, page, pageSize), ct));
}
