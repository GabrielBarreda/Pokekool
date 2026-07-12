namespace Pokekool.Domain.Entities.Storage;

/// <summary>Origine de l'étiquette d'un carton de rangement.</summary>
public enum LabelOrigin
{
    /// <summary>Carton fournisseur : identifié par un code-barre imprimé (<see cref="StorageBox.Barcode"/>).</summary>
    Supplier = 1,

    /// <summary>Carton interne boutique : identifié par un code interne (<see cref="StorageBox.InternalCode"/>).</summary>
    Internal = 2,
}

/// <summary>
/// Cycle de vie d'un déplacement de carton détecté (par caméra, futur prompt).
/// Jamais appliqué automatiquement : validation humaine obligatoire.
/// </summary>
public enum MovementStatus
{
    Pending   = 1,
    Confirmed = 2,
    Rejected  = 3,
}

/// <summary>Statut d'un job de compilation Blender (.blend → .glb).</summary>
public enum CompileJobStatus
{
    Queued    = 1,
    Running   = 2,
    Succeeded = 3,
    Failed    = 4,
}

/// <summary>Type de meuble de rangement (modèle 3D fixe associé).</summary>
public enum FurnitureType
{
    /// <summary>Étagère classique verticale, 6 niveaux.</summary>
    ShelfClassic6 = 1,
    /// <summary>Étagère murale longue, un seul niveau.</summary>
    ShelfWall     = 2,
}
