using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.UpdateAppointmentStatus;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class UpdateAppointmentStatusActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "update_appointment_status";
    public string Description => "Change le statut d'un RDV (Confirmed, Cancelled, Completed, Pending). REQUIERT confirmation.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            appointmentId = new { type = "string", description = "UUID du RDV" },
            status = new { type = "string", description = "Confirmed | Cancelled | Completed | Pending | InProgress" }
        },
        required = new[] { "appointmentId", "status" }
    };

    public UpdateAppointmentStatusActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "appointmentId")
                 ?? throw new ArgumentException("appointmentId invalide.");
        var status = ChatArg.String(args, "status") ?? throw new ArgumentException("status requis.");
        var result = await _mediator.Send(new UpdateAppointmentStatusCommand(id, status), ct);
        return $"✅ RDV {result.Id} → statut '{result.Status}'";
    }
}
