namespace Pokekool.Shared.Dtos.Storage;

public sealed record StorageRoomDto(
    Guid Id,
    Guid ShopId,
    string Name,
    string? FloorPlanUrl,
    string? Model3dUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record StorageBoxDto(
    Guid Id,
    Guid RoomId,
    string LabelOrigin,
    string? Barcode,
    string? InternalCode,
    string? SupplierName,
    string? Size,
    string? Position3dJson,
    string? Notes,
    Guid? FurnitureId,
    short? LevelIndex,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record StorageBinderDto(
    Guid Id,
    Guid? BoxId,
    string Label,
    short? PositionInBox,
    Guid? FurnitureId,
    short? LevelIndex,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record StorageFurnitureDto(
    Guid Id,
    Guid RoomId,
    string Type,
    string? Name,
    int LevelCount,
    string? Position3dJson,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record StorageLocationDto(
    Guid Id,
    Guid BinderId,
    string OwnerType,
    Guid OwnerId,
    short? SlotPosition,
    DateTime MovedAt);

/// <summary>Salle + meubles + cartons (avec classeurs) — payload de chargement de la vue 3D.</summary>
public sealed record StorageRoomLayoutDto(
    StorageRoomDto Room,
    IReadOnlyList<StorageBoxWithBindersDto> Boxes,
    IReadOnlyList<StorageFurnitureDto> Furniture,
    IReadOnlyList<StorageBinderDto> LooseBinders);

public sealed record StorageBoxWithBindersDto(
    StorageBoxDto Box,
    IReadOnlyList<StorageBinderDto> Binders);

/// <summary>Résultat d'un lookup par code : carton + classeurs + localisations théoriques.</summary>
public sealed record StorageBoxLookupDto(
    StorageBoxDto Box,
    IReadOnlyList<StorageBinderWithLocationsDto> Binders);

public sealed record StorageBinderWithLocationsDto(
    StorageBinderDto Binder,
    IReadOnlyList<StorageLocationDto> Locations);

public sealed record StorageMovementEventDto(
    Guid Id,
    Guid? BoxId,
    DateTime DetectedAt,
    string? FromPosition3dJson,
    string? ToPosition3dJson,
    string Status,
    Guid? ConfirmedBy,
    DateTime? ConfirmedAt,
    DateTimeOffset CreatedAt);

/// <summary>Statut d'un job de compilation Blender (polling front).</summary>
public sealed record StorageCompileJobDto(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string Status,
    string? GlbOutputPath,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTime? CompletedAt);
