using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Shop;

/// <summary>
/// Rendez-vous client pour une prestation (pré-grading, restauration, rachat...).
/// </summary>
public sealed class ShopAppointment : Entity
{
    public Guid ShopId { get; private set; }
    public string ClientName { get; private set; } = string.Empty;
    public string ClientEmail { get; private set; } = string.Empty;
    public string? ClientPhone { get; private set; }

    public ShopServiceType ServiceType { get; private set; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public AppointmentStatus Status { get; private set; }

    public string? NotesClient { get; private set; }
    public string? NotesInternal { get; private set; }

    /// <summary>Futur planning employés — employé assigné au RDV.</summary>
    public Guid? AssignedToUserId { get; private set; }

    private ShopAppointment() { }

    public static ShopAppointment Create(
        Guid shopId,
        string clientName,
        string clientEmail,
        string? clientPhone,
        ShopServiceType serviceType,
        DateTimeOffset scheduledAt,
        int durationMinutes,
        AppointmentStatus status,
        string? notesClient,
        string? notesInternal,
        Guid? assignedToUserId)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("ClientName is required", nameof(clientName));
        if (string.IsNullOrWhiteSpace(clientEmail))
            throw new ArgumentException("ClientEmail is required", nameof(clientEmail));
        if (durationMinutes <= 0)
            throw new ArgumentException("DurationMinutes must be > 0", nameof(durationMinutes));

        return new ShopAppointment
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            ClientName = clientName.Trim(),
            ClientEmail = clientEmail.Trim(),
            ClientPhone = clientPhone,
            ServiceType = serviceType,
            ScheduledAt = scheduledAt,
            DurationMinutes = durationMinutes,
            Status = status,
            NotesClient = notesClient,
            NotesInternal = notesInternal,
            AssignedToUserId = assignedToUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Modifie le créneau, la prestation et les notes internes.</summary>
    public void UpdateDetails(
        DateTimeOffset scheduledAt,
        int durationMinutes,
        ShopServiceType serviceType,
        string? notesInternal,
        Guid? assignedToUserId)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("DurationMinutes must be > 0", nameof(durationMinutes));

        ScheduledAt = scheduledAt;
        DurationMinutes = durationMinutes;
        ServiceType = serviceType;
        NotesInternal = notesInternal;
        AssignedToUserId = assignedToUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeStatus(AppointmentStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
