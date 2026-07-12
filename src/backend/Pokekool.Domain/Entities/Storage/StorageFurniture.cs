using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Meuble de rangement posé dans une <see cref="StorageRoom"/> (étagère). Reçoit des cartons
/// et/ou des classeurs sur ses niveaux. Le modèle 3D est fixe (choisi via <see cref="Type"/>),
/// aucun upload : seul le placement est stocké.
/// </summary>
public sealed class StorageFurniture : Entity
{
    public Guid RoomId { get; private set; }
    public FurnitureType Type { get; private set; }
    public string? Name { get; private set; }

    /// <summary>Position dans la vue 3D, JSON : { x, y, z, rotation }.</summary>
    public string? Position3dJson { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Nombre de niveaux, dérivé du type (6 pour la classique, 1 pour la murale).</summary>
    public int LevelCount => Type == FurnitureType.ShelfClassic6 ? 6 : 1;

    private StorageFurniture() { }

    public static StorageFurniture Create(Guid roomId, FurnitureType type, string? name, string? position3dJson = null)
    {
        return new StorageFurniture
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Type = type,
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            Position3dJson = string.IsNullOrWhiteSpace(position3dJson) ? null : position3dJson.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string? name, string? notes)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPosition3d(string? position3dJson)
    {
        Position3dJson = string.IsNullOrWhiteSpace(position3dJson) ? null : position3dJson.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Valide qu'un index de niveau est dans les bornes du meuble.</summary>
    public bool IsValidLevel(short levelIndex) => levelIndex >= 0 && levelIndex < LevelCount;
}
