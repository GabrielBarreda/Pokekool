namespace Pokekool.Shared.Requests.Storage;

public sealed record CreateStorageRoomRequest(string Name);

public sealed record CreateStorageBoxRequest(
    Guid RoomId,
    string LabelOrigin,        // "Supplier" | "Internal"
    string? Barcode,
    string? InternalCode,
    string? SupplierName,
    string? Size,
    string? Position3dJson,
    string? Notes);

public sealed record UpdateBoxPositionRequest(string? Position3dJson);

/// <summary>Crée un classeur : soit dans un carton (BoxId), soit sur un meuble (FurnitureId + LevelIndex).</summary>
public sealed record CreateStorageBinderRequest(
    string Label,
    Guid? BoxId,
    short? PositionInBox,
    Guid? FurnitureId,
    short? LevelIndex);

public sealed record AssignStorageLocationRequest(
    Guid BinderId,
    string OwnerType,          // "shop_stock" | "user_collection"
    Guid OwnerId,
    short? SlotPosition);

/// <summary>Assigne en lot plusieurs items du stock boutique à un classeur.</summary>
public sealed record BatchAssignBinderRequest(IReadOnlyList<Guid> OwnerIds);

/// <summary>Crée des StorageLocations depuis un scan validé (cardIds = ShopStock.Id).</summary>
public sealed record CreateFromScanRequest(
    Guid BinderId,
    string OwnerType,
    IReadOnlyList<Guid> ValidatedOwnerIds);

public sealed record ConfirmMovementRequest(bool Confirm);

// ── Meubles / rangement ──────────────────────────────────────────────────────
public sealed record CreateStorageFurnitureRequest(
    Guid RoomId,
    string Type,               // "ShelfClassic6" | "ShelfWall"
    string? Name);

public sealed record UpdateFurniturePositionRequest(string? Position3dJson);

/// <summary>Où vit un carton : sur un meuble (FurnitureId+LevelIndex) ou au sol (Position3dJson). Tous nuls = détaché.</summary>
public sealed record PlaceBoxRequest(
    Guid? FurnitureId,
    short? LevelIndex,
    string? Position3dJson);

/// <summary>Où vit un classeur : dans un carton (BoxId) ou sur un meuble (FurnitureId+LevelIndex).</summary>
public sealed record PlaceBinderRequest(
    Guid? BoxId,
    short? PositionInBox,
    Guid? FurnitureId,
    short? LevelIndex);

/// <summary>Mise à jour des paramètres du module Stockage.</summary>
public sealed record UpdateStorageSettingsRequest(
    bool CascadeDelete);
