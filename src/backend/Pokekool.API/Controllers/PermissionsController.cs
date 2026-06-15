using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Authorization.Queries;
using Pokekool.Shared.Dtos.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[Route("api/v{version:apiVersion}/permissions")]
public sealed class PermissionsController : BaseApiController
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Catalogue des permissions",
        Description = "Liste exhaustive des permissions disponibles. Utilisée par l'UI pour construire l'écran d'édition des rôles.")]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> List(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ListPermissionsQuery(), cancellationToken));
}
