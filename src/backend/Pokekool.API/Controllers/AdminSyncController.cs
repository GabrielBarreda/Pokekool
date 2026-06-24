using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Pokemon.Commands.SyncCardPopulation;
using Pokekool.Application.Pokemon.Commands.SyncCardPrices;
using Pokekool.Application.Pokemon.Commands.SyncPokemonTcg;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Pokemon;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/sync")]
[Produces("application/json")]
[Authorize]
public sealed class AdminSyncController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminSyncController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Synchronise le catalogue Pokémon TCG (séries, sets, cartes, prix embarqués).
    /// </summary>
    [HttpPost("catalog")]
    [RequirePermission(Permissions.CardsSync)]
    [ProducesResponseType(typeof(SyncResultDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Sync Pokemon TCG catalog (series, sets, cards)")]
    public async Task<ActionResult<SyncResultDto>> SyncCatalog(
        [FromQuery] bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SyncPokemonTcgCommand(forceRefresh), ct);
        return Ok(result);
    }

    /// <summary>
    /// Synchronise les prix des cartes via PriceCharting.
    /// Body { cardIds: [] } — vide = toutes les cartes.
    /// </summary>
    [HttpPost("prices")]
    [RequirePermission(Permissions.CardsSync)]
    [ProducesResponseType(typeof(SyncPricesResultDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Sync card prices via PriceCharting")]
    public async Task<ActionResult<SyncPricesResultDto>> SyncPrices(
        [FromBody] SyncPricesRequest? body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new SyncCardPricesCommand(body?.CardIds, body?.StalenessHours ?? 24), ct);
        return Ok(result);
    }

    /// <summary>
    /// Synchronise les données de population PSA des cartes.
    /// Body { cardIds: [] } — vide = toutes les cartes.
    /// </summary>
    [HttpPost("population")]
    [RequirePermission(Permissions.CardsSync)]
    [ProducesResponseType(typeof(SyncPopulationResultDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Sync card population data via PSA")]
    public async Task<ActionResult<SyncPopulationResultDto>> SyncPopulation(
        [FromBody] SyncPopulationRequest? body,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new SyncCardPopulationCommand(body?.CardIds), ct);
        return Ok(result);
    }
}

/// <param name="CardIds">IDs explicites — null = toutes les cartes périmées.</param>
/// <param name="StalenessHours">Fraîcheur en heures (défaut 24). 0 = force-sync total.</param>
public sealed record SyncPricesRequest(
    IEnumerable<Guid>? CardIds        = null,
    int                StalenessHours = 24);
public sealed record SyncPopulationRequest(IEnumerable<Guid>? CardIds = null);
