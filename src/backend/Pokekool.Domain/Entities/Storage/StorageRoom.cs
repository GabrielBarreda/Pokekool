using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Salle / local physique de stockage de la boutique. Contient des cartons (<see cref="StorageBox"/>)
/// et porte le modèle 3D (.glb) affiché dans le viewer three.js.
/// </summary>
public sealed class StorageRoom : Entity
{
    public Guid ShopId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL éventuelle d'un plan 2D (image).</summary>
    public string? FloorPlanUrl { get; private set; }

    /// <summary>URL du modèle 3D compilé (.glb) consommé par three.js. Null tant qu'aucun .blend n'a été compilé.</summary>
    public string? Model3dUrl { get; private set; }

    private StorageRoom() { }

    public static StorageRoom Create(Guid shopId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("storage.room.name_required", "Le nom de la salle est requis.");

        return new StorageRoom
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            Name = name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("storage.room.name_required", "Le nom de la salle est requis.");
        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetModel3dUrl(string? url)
    {
        Model3dUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetFloorPlanUrl(string? url)
    {
        FloorPlanUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
