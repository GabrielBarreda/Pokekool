using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Classeur rangé dans un <see cref="StorageBox"/>. Reçoit des localisations d'items (<see cref="StorageLocation"/>).
/// </summary>
public sealed class StorageBinder : Entity
{
    public Guid BoxId { get; private set; }
    public string Label { get; private set; } = string.Empty;

    /// <summary>Position ordinale du classeur dans le carton (optionnelle).</summary>
    public short? PositionInBox { get; private set; }

    private StorageBinder() { }

    public static StorageBinder Create(Guid boxId, string label, short? positionInBox)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("storage.binder.label_required", "Le libellé du classeur est requis.");

        return new StorageBinder
        {
            Id = Guid.NewGuid(),
            BoxId = boxId,
            Label = label.Trim(),
            PositionInBox = positionInBox,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string label, short? positionInBox)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("storage.binder.label_required", "Le libellé du classeur est requis.");
        Label = label.Trim();
        PositionInBox = positionInBox;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
