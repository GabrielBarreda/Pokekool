namespace Pokekool.Shared.Requests.Shop;

/// <summary>Modification d'un créneau + notes internes d'un RDV.</summary>
public sealed record UpdateAppointmentRequest(
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string ServiceType,
    string? NotesInternal,
    Guid? AssignedToUserId);

public sealed record UpdateAppointmentStatusRequest(string Status);
