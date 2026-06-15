using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Authorization.Commands;
using Pokekool.Application.Authorization.Queries;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController : BaseApiController
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [RequirePermission(Permissions.RolesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List roles", Description = "Rôles système + rôles propres au tenant.")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ListRolesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.RolesRead)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Get role")]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetRoleByIdQuery(id), cancellationToken));

    [HttpPost]
    [RequirePermission(Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Create custom role",
        Description = "Vous ne pouvez attribuer que des permissions que vous possédez vous-même (anti-escalation).")]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateRoleCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1" }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Update custom role", Description = "Interdit sur les rôles système.")]
    public async Task<ActionResult<RoleDto>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new UpdateRoleCommand(id, request), cancellationToken));

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.RolesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Delete custom role", Description = "Interdit sur les rôles système.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return NoContent();
    }
}
