using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Pokemon.Commands.SyncCardPopulation;
using Pokekool.Application.Pokemon.Commands.SyncCardPrices;
using Pokekool.Application.Pokemon.Commands.SyncPokemonTcg;
using Pokekool.Infrastructure.Services;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Pokemon;
using Pokekool.Shared.Dtos.Sync;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/sync")]
[Produces("application/json")]
[Authorize]
public sealed class AdminSyncController : BaseApiController
{
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IMediator            _mediator;
    private readonly SyncProgressService  _progressService;
    private readonly SyncScheduleState    _scheduleState;

    public AdminSyncController(
        IMediator mediator,
        SyncProgressService progressService,
        SyncScheduleState scheduleState)
    {
        _mediator        = mediator;
        _progressService = progressService;
        _scheduleState   = scheduleState;
    }

    /// <summary>
    /// Synchronise le catalogue Pokémon TCG (séries, sets, cartes, prix embarqués).
    /// </summary>
    [HttpPost("catalog")]
    [RequirePermission(Permissions.CardsSync)]
    [ProducesResponseType(typeof(SyncResultDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Sync Pokemon TCG catalog (series, sets, cards)")]
    public async Task<ActionResult<SyncResultDto>> SyncCatalog(
        [FromQuery] bool forceRefresh   = false,
        [FromQuery] string? setTcgId    = null,
        CancellationToken ct            = default)
    {
        var result = await _mediator.Send(new SyncPokemonTcgCommand(forceRefresh, setTcgId), ct);
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
            new SyncCardPricesCommand(body?.CardIds, body?.StalenessHours ?? 24, body?.SetId), ct);
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

    // ── Sync planifié ──────────────────────────────────────────────────────────

    /// <summary>Programme un sync prix global à l'heure indiquée (HH:mm, heure locale).</summary>
    [HttpPost("schedule")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Planifie un sync prix global")]
    public IActionResult ScheduleSync([FromBody] ScheduleSyncRequest req)
    {
        if (!TimeOnly.TryParse(req.Time, out var time))
            return BadRequest("Format invalide — attendu HH:mm.");

        var now     = DateTime.Now;
        var todayRun = DateTime.Today.Add(time.ToTimeSpan());
        var nextRun  = todayRun > now ? todayRun : todayRun.AddDays(1);

        _scheduleState.ScheduledTime = time;
        _scheduleState.IsRecurring   = req.IsRecurring;
        _scheduleState.NextRunAt     = nextRun.ToUniversalTime();
        _scheduleState.Status        = SyncScheduleStatus.Pending;

        return Ok(MapScheduleToDto());
    }

    /// <summary>Retourne l'état du sync planifié.</summary>
    [HttpGet("schedule")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "État du sync planifié")]
    public IActionResult GetSchedule() => Ok(MapScheduleToDto());

    /// <summary>Annule le sync planifié.</summary>
    [HttpDelete("schedule")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Annule le sync planifié")]
    public IActionResult CancelSchedule()
    {
        _scheduleState.Status    = SyncScheduleStatus.Cancelled;
        _scheduleState.NextRunAt = null;
        return Ok(MapScheduleToDto());
    }

    // ── SSE progress ───────────────────────────────────────────────────────────

    /// <summary>
    /// Server-Sent Events — stream de progression du sync prix en cours.
    /// Le client se connecte et reçoit un event JSON par carte traitée.
    /// </summary>
    [HttpGet("progress")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)] // SSE — retourne Task brut, non représentable en OpenAPI
    public async Task GetSyncProgress(CancellationToken ct)
    {
        Response.Headers["Content-Type"]      = "text/event-stream";
        Response.Headers["Cache-Control"]     = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["Connection"]        = "keep-alive";

        await foreach (var evt in _progressService.SubscribeAsync(ct))
        {
            var json = JsonSerializer.Serialize(evt, SseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private SyncScheduleDto MapScheduleToDto() => new(
        ScheduledTime:     _scheduleState.ScheduledTime?.ToString("HH:mm"),
        IsRecurring:       _scheduleState.IsRecurring,
        NextRunAt:         _scheduleState.NextRunAt,
        Status:            _scheduleState.Status.ToString().ToLowerInvariant(),
        LastCompletedAt:   _scheduleState.LastCompletedAt,
        LastPricesUpserted: _scheduleState.LastPricesUpserted);
}

/// <param name="CardIds">IDs explicites — null = scope défini par SetId ou global.</param>
/// <param name="StalenessHours">Fraîcheur en heures (défaut 24). 0 = force-sync du scope.</param>
/// <param name="SetId">DB GUID du set à synchroniser (null = global).</param>
public sealed record SyncPricesRequest(
    IEnumerable<Guid>? CardIds        = null,
    int                StalenessHours = 24,
    Guid?              SetId          = null);
public sealed record SyncPopulationRequest(IEnumerable<Guid>? CardIds = null);

/// <param name="Time">Heure locale au format HH:mm (ex. "02:30").</param>
/// <param name="IsRecurring">Si true, relance automatiquement chaque nuit à la même heure.</param>
public sealed record ScheduleSyncRequest(string Time, bool IsRecurring = false);
