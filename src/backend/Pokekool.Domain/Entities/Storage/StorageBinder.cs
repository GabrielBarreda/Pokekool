using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Classeur. Il vit soit <b>dans un carton</b> (<see cref="BoxId"/>), soit <b>directement sur un
/// meuble/niveau</b> (<see cref="FurnitureId"/> + <see cref="LevelIndex"/>), soit non rangé.
/// Au plus un rattachement à la fois. Reçoit des localisations d'items (<see cref="StorageLocation"/>).
/// </summary>
public sealed class StorageBinder : Entity
{
    /// <summary>Carton conteneur (null si le classeur est posé directement sur un meuble ou non rangé).</summary>
    public Guid? BoxId { get; private set; }
    public string Label { get; private set; } = string.Empty;

    /// <summary>Position ordinale du classeur dans le carton (optionnelle).</summary>
    public short? PositionInBox { get; private set; }

    /// <summary>Meuble sur lequel le classeur est posé directement (null si dans un carton).</summary>
    public Guid? FurnitureId { get; private set; }

    /// <summary>Niveau du meuble (0-based) si posé directement sur un meuble.</summary>
    public short? LevelIndex { get; private set; }

    private StorageBinder() { }

    /// <summary>Crée un classeur dans un carton.</summary>
    public static StorageBinder CreateInBox(Guid boxId, string label, short? positionInBox)
    {
        var binder = NewWithLabel(label);
        binder.BoxId = boxId;
        binder.PositionInBox = positionInBox;
        return binder;
    }

    /// <summary>Crée un classeur posé directement sur un meuble/niveau.</summary>
    public static StorageBinder CreateOnFurniture(Guid furnitureId, short levelIndex, string label)
    {
        if (levelIndex < 0)
            throw new DomainException("storage.binder.invalid_level", "Niveau invalide.");
        var binder = NewWithLabel(label);
        binder.FurnitureId = furnitureId;
        binder.LevelIndex = levelIndex;
        return binder;
    }

    private static StorageBinder NewWithLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("storage.binder.label_required", "Le libellé du classeur est requis.");
        return new StorageBinder
        {
            Id = Guid.NewGuid(),
            Label = label.Trim(),
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

    /// <summary>Range le classeur dans un carton (détache d'un éventuel meuble).</summary>
    public void PlaceInBox(Guid boxId, short? positionInBox)
    {
        BoxId = boxId;
        PositionInBox = positionInBox;
        FurnitureId = null;
        LevelIndex = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Pose le classeur directement sur un meuble/niveau (détache d'un éventuel carton).</summary>
    public void PlaceOnFurniture(Guid furnitureId, short levelIndex)
    {
        if (levelIndex < 0)
            throw new DomainException("storage.binder.invalid_level", "Niveau invalide.");
        FurnitureId = furnitureId;
        LevelIndex = levelIndex;
        BoxId = null;
        PositionInBox = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
