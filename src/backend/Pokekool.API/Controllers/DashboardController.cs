using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Dashboard.Queries.GetDashboardSummary;
using Pokekool.Shared.Dtos.Dashboard;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController : BaseApiController
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Agrège en un seul appel toutes les données nécessaires au tableau de bord.
    /// </summary>
    [HttpGet("summary")]
    [Authorize]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary     = "Résumé du tableau de bord",
        Description = "Retourne les cartes récemment scrapées (24h), le nombre de collectionneurs actifs, " +
                      "et des stubs neutres pour les modules non encore implémentés " +
                      "(ventes, messagerie, IA).")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDashboardSummaryQuery(), ct));
}
