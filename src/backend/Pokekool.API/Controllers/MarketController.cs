using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Pokemon.Queries.GetCardGrading;
using Pokekool.Application.Pokemon.Queries.GetMarketCards;
using Pokekool.Application.Pokemon.Queries.GetSetFilters;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;
using Asp.Versioning;
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

    /// <summary>
    /// Liste toutes les cartes du catalogue, tous sets confondus (mode "Tous les sets" du
    /// sélecteur front — 2026-09-08), avec leurs prix de marché résumés. Même filtres/pagination
    /// que GetMarketCards, sans restriction de set.
    /// </summary>
    [HttpGet("cards")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<MarketCardDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Liste des cartes du marché, tous sets confondus",
        Description = "Retourne les cartes du catalogue entier enrichies du meilleur prix marché disponible (tcgplayer holofoil > normal > cardmarket).")]
    public async Task<ActionResult<PagedResult<MarketCardDto>>> GetAllMarketCards(
        [FromQuery] string? search,
        [FromQuery] string? rarity,
        [FromQuery] string? supertype,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 60,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(
            new GetMarketCardsQuery(SetId: null, search, rarity, supertype, page, pageSize), ct));

    /// <summary>
    /// Données de gradation réelles pour une carte (PSA via PriceCharting).
    /// Retourne une liste vide si aucune donnée n'est disponible.
    /// </summary>
    [HttpGet("cards/{cardId:guid}/grading")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CardGradingDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Données de gradation PSA pour une carte")]
    public async Task<ActionResult<IReadOnlyList<CardGradingDto>>> GetCardGrading(
        Guid cardId,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCardGradingQuery(cardId), ct));
}
