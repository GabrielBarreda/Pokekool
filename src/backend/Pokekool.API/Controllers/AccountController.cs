using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Account.Commands;
using Pokekool.Application.Users.Commands;
using Pokekool.Shared.Dtos.Users;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

/// <summary>
/// Paramètres du compte du client authentifié : mot de passe, email, suppression.
/// </summary>
[Route("api/v{version:apiVersion}/account")]
[Authorize]
public sealed class AccountController : BaseApiController
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator) => _mediator = mediator;

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Change my password", Description = "Requires current password.")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        await _mediator.Send(new ChangePasswordCommand(request), ct);
        return NoContent();
    }

    [HttpPost("change-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Change my email")]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequest request,
        CancellationToken ct)
    {
        await _mediator.Send(new ChangeEmailCommand(request.NewEmail), ct);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Delete my account", Description = "Soft-deletes the authenticated user's account.")]
    public async Task<IActionResult> DeleteMyAccount(CancellationToken ct)
    {
        await _mediator.Send(new DeleteMyAccountCommand(), ct);
        return NoContent();
    }
}

public sealed record ChangeEmailRequest(string NewEmail);
