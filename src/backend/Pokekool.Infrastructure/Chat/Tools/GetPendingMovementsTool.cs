using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Queries;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>Liste les mouvements de stockage détectés en attente de confirmation (lecture seule).</summary>
public sealed class GetPendingMovementsTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "get_pending_movements";
    public string Description => "Liste les déplacements de cartons détectés, en attente de confirmation par un opérateur.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new { }
    };

    public GetPendingMovementsTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var events = await _mediator.Send(new GetPendingMovementsQuery(), ct);
        if (events.Count == 0)
            return "Aucun mouvement en attente.";

        var lines = events.Select(e =>
            $"- [{e.Id}] carton {e.BoxId?.ToString() ?? "?"} · détecté {e.DetectedAt:dd/MM HH:mm} · {e.Status}");
        return $"Mouvements en attente ({events.Count}) :\n" + string.Join("\n", lines);
    }
}
