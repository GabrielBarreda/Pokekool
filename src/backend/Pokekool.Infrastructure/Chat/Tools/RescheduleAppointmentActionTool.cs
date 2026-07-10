using System.Globalization;
using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.UpdateAppointment;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class RescheduleAppointmentActionTool : IChatTool
{
    private readonly IMediator _mediator;
    private readonly IShopAppointmentRepository _appointments;

    public string Name => "reschedule_appointment";
    public string Description => "Déplace un RDV à un autre créneau horaire. REQUIERT confirmation.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            appointmentId = new { type = "string" },
            newScheduledAt = new { type = "string", description = "ISO 8601 datetime (ex: '2026-07-10T14:00:00Z')" },
            durationMinutes = new { type = "integer", description = "Durée en minutes (défaut: 60)" }
        },
        required = new[] { "appointmentId", "newScheduledAt" }
    };

    public RescheduleAppointmentActionTool(IMediator mediator, IShopAppointmentRepository appointments)
    {
        _mediator = mediator;
        _appointments = appointments;
    }

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "appointmentId")
                 ?? throw new ArgumentException("appointmentId invalide.");

        var newSlotRaw = ChatArg.String(args, "newScheduledAt");
        if (!DateTimeOffset.TryParse(newSlotRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var newSlot))
            throw new ArgumentException("newScheduledAt invalide (attendu ISO 8601).");

        // On recharge le RDV pour préserver le type de prestation et les notes internes.
        var existing = await _appointments.GetByIdAsync(id, ct)
                       ?? throw new ArgumentException($"RDV {id} introuvable.");

        var duration = ChatArg.Int(args, "durationMinutes", existing.DurationMinutes > 0 ? existing.DurationMinutes : 60);

        var result = await _mediator.Send(new UpdateAppointmentCommand(
            Id: id,
            ScheduledAt: newSlot,
            DurationMinutes: duration,
            ServiceType: existing.ServiceType.ToString(),
            NotesInternal: existing.NotesInternal,
            AssignedToUserId: existing.AssignedToUserId), ct);

        return $"✅ RDV {result.Id} déplacé au {result.ScheduledAt:dd/MM/yyyy HH:mm}";
    }
}
