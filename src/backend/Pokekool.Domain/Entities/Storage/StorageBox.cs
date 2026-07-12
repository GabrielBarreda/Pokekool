using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Carton physique rangé dans une <see cref="StorageRoom"/>. Contient des classeurs (<see cref="StorageBinder"/>).
/// Identifié soit par un code-barre fournisseur, soit par un code interne, selon <see cref="LabelOrigin"/>.
/// </summary>
public sealed class StorageBox : Entity
{
    public Guid RoomId { get; private set; }
    public LabelOrigin LabelOrigin { get; private set; }

    /// <summary>Code-barre — requis (et seul renseigné) si <see cref="LabelOrigin.Supplier"/>.</summary>
    public string? Barcode { get; private set; }

    /// <summary>Code interne — requis (et seul renseigné) si <see cref="LabelOrigin.Internal"/>.</summary>
    public string? InternalCode { get; private set; }

    public string? SupplierName { get; private set; }

    /// <summary>Taille du carton : 'S' | 'M' | 'L' | 'XL'.</summary>
    public string? Size { get; private set; }

    /// <summary>Position dans la vue 3D, JSON : { x, y, z, rotation }.</summary>
    public string? Position3dJson { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Meuble sur lequel le carton est rangé (null = posé librement au sol via <see cref="Position3dJson"/>).</summary>
    public Guid? FurnitureId { get; private set; }

    /// <summary>Niveau du meuble (0-based) si rangé sur un meuble.</summary>
    public short? LevelIndex { get; private set; }

    private StorageBox() { }

    public static StorageBox Create(
        Guid roomId,
        LabelOrigin labelOrigin,
        string? barcode,
        string? internalCode,
        string? supplierName,
        string? size,
        string? position3dJson,
        string? notes)
    {
        var box = new StorageBox
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        box.Apply(labelOrigin, barcode, internalCode, supplierName, size, position3dJson, notes);
        return box;
    }

    public void Update(
        LabelOrigin labelOrigin,
        string? barcode,
        string? internalCode,
        string? supplierName,
        string? size,
        string? position3dJson,
        string? notes)
    {
        Apply(labelOrigin, barcode, internalCode, supplierName, size, position3dJson, notes);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Met à jour uniquement la position 3D (déplacement du carton dans le viewer).</summary>
    public void SetPosition3d(string? position3dJson)
    {
        Position3dJson = string.IsNullOrWhiteSpace(position3dJson) ? null : position3dJson.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Range le carton sur un meuble/niveau. Exclusif de la pose libre au sol.</summary>
    public void PlaceOnFurniture(Guid furnitureId, short levelIndex)
    {
        if (levelIndex < 0)
            throw new DomainException("storage.box.invalid_level", "Niveau invalide.");
        FurnitureId = furnitureId;
        LevelIndex = levelIndex;
        Position3dJson = null; // la position est dérivée du meuble + niveau côté viewer
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Repose le carton librement au sol (détache du meuble). Position 3D optionnelle.</summary>
    public void PlaceOnFloor(string? position3dJson)
    {
        FurnitureId = null;
        LevelIndex = null;
        Position3dJson = string.IsNullOrWhiteSpace(position3dJson) ? Position3dJson : position3dJson.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(
        LabelOrigin labelOrigin,
        string? barcode,
        string? internalCode,
        string? supplierName,
        string? size,
        string? position3dJson,
        string? notes)
    {
        barcode = Normalize(barcode);
        internalCode = Normalize(internalCode);

        // Règle métier : Barcode XOR InternalCode selon l'origine de l'étiquette.
        if (labelOrigin == LabelOrigin.Supplier)
        {
            if (barcode is null)
                throw new DomainException("storage.box.barcode_required",
                    "Un code-barre est requis pour un carton fournisseur.");
            internalCode = null;
        }
        else // Internal
        {
            if (internalCode is null)
                throw new DomainException("storage.box.internal_code_required",
                    "Un code interne est requis pour un carton interne.");
            barcode = null;
        }

        if (size is not null && size is not ("S" or "M" or "L" or "XL"))
            throw new DomainException("storage.box.invalid_size", "Taille invalide (S, M, L ou XL attendu).");

        LabelOrigin = labelOrigin;
        Barcode = barcode;
        InternalCode = internalCode;
        SupplierName = Normalize(supplierName);
        Size = size;
        Position3dJson = Normalize(position3dJson);
        Notes = notes;
    }

    private static string? Normalize(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
