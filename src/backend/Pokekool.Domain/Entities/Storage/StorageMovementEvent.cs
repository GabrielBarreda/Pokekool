using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Déplacement de carton détecté (future détection caméra — hors périmètre de ce prompt).
/// Ce prompt ne fournit que le modèle + l'écran de confirmation humaine. Un événement
/// n'est <b>jamais</b> appliqué automatiquement : un opérateur le confirme ou le rejette.
/// </summary>
public sealed class StorageMovementEvent : Entity
{
    public Guid? BoxId { get; private set; }
    public DateTime DetectedAt { get; private set; }

    public string? FromPosition3dJson { get; private set; }
    public string? ToPosition3dJson { get; private set; }

    public MovementStatus Status { get; private set; } = MovementStatus.Pending;
    public Guid? ConfirmedBy { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    private StorageMovementEvent() { }

    public static StorageMovementEvent Create(
        Guid? boxId,
        DateTime detectedAt,
        string? fromPosition3dJson,
        string? toPosition3dJson)
    {
        return new StorageMovementEvent
        {
            Id = Guid.NewGuid(),
            BoxId = boxId,
            DetectedAt = detectedAt,
            FromPosition3dJson = fromPosition3dJson,
            ToPosition3dJson = toPosition3dJson,
            Status = MovementStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Confirm(Guid userId)
    {
        EnsurePending();
        Status = MovementStatus.Confirmed;
        ConfirmedBy = userId;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(Guid userId)
    {
        EnsurePending();
        Status = MovementStatus.Rejected;
        ConfirmedBy = userId;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != MovementStatus.Pending)
            throw new DomainException("storage.movement.already_resolved",
                "Cet événement de déplacement a déjà été traité.");
    }
}
