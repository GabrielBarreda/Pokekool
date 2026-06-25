using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Users.Commands;
using Pokekool.Application.Users.Queries;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Users;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController : BaseApiController
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [RequirePermission(Permissions.UsersRead)]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "List users (paginé, filtré)")]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] string? roleCode = null,
        [FromQuery] bool? hasPendingMessages = null,
        [FromQuery] bool? recentOnly = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new GetUsersQuery(search, roleCode, hasPendingMessages, recentOnly, sortBy, page, pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.UsersRead)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Get user")]
    public async Task<ActionResult<UserDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetUserByIdQuery(id), cancellationToken));

    [HttpPost]
    [RequirePermission(Permissions.UsersWrite)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Create user",
        Description = "Crée un user avec mot de passe initial. Anti-escalation : les rôles attribués ne peuvent contenir que des permissions que vous possédez.")]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateUserCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1" }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.UsersWrite)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Update user profile (firstName, lastName, phone)")]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new UpdateUserCommand(id, request), cancellationToken));

    [HttpPut("{id:guid}/roles")]
    [RequirePermission(Permissions.UsersAssignRoles)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Assign roles (replaces the current set)",
        Description = "Remplace l'ensemble des rôles d'un user. Idempotent. Anti-escalation appliquée.")]
    public async Task<ActionResult<UserDto>> AssignRoles(
        Guid id,
        [FromBody] AssignRolesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AssignRolesCommand(id, request), cancellationToken));

    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(Permissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Activate user")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetUserActivationCommand(id, true), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(Permissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Deactivate user", Description = "Refusé si c'est le dernier admin actif du tenant.")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetUserActivationCommand(id, false), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.UsersDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Soft-delete user")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(Permissions.UsersResetPassword)]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Reset password (admin)",
        Description = "Génère un mot de passe temporaire que l'admin doit communiquer au user.")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ResetPasswordCommand(id), cancellationToken));
}
