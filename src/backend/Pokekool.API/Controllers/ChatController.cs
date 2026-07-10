using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Chat;
using Pokekool.Shared.Dtos.Chat;

namespace Pokekool.API.Controllers;

/// <summary>ChatBot IA — admin et employés uniquement.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chat")]
[Authorize]
public sealed class ChatController : BaseApiController
{
    private readonly IChatOrchestrator _orchestrator;

    public ChatController(IChatOrchestrator orchestrator) => _orchestrator = orchestrator;

    /// <summary>Envoie un message au chatbot. Retourne la réponse + éventuelle action en attente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponseDto>> Chat(
        [FromBody] ChatRequestDto request,
        CancellationToken ct = default)
        => Ok(await _orchestrator.ProcessAsync(request, ct));
}
