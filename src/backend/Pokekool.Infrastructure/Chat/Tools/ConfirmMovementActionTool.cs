using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Commands;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Confirme ou rejette un mouvement de stockage détecté. REQUIERT confirmation explicite.
/// </summary>
public sealed class ConfirmMovementActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "confirm_movement";
    public string Description =>
        "Confirme (confirm=true) ou rejette (confirm=false) un mouvement de carton détecté. " +
        "Utilise get_pending_movements pour l'eventId. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            eventId = new { type = "string", description = "UUID du mouvement détecté" },
            confirm = new { type = "boolean", description = "true = confirmer, false = rejeter" }
        },
        required = new[] { "eventId", "confirm" }
    };

    public ConfirmMovementActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "eventId")
                 ?? throw new ArgumentException("eventId invalide.");
        var confirm = ChatArg.Bool(args, "confirm", true);
        var result = await _mediator.Send(new CreateMovementConfirmationCommand(id, confirm), ct);
        return $"✅ Mouvement {result.Id} → {result.Status}";
    }
}
