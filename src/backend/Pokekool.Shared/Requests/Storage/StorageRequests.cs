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

public sealed record CreateStorageBinderRequest(
    Guid BoxId,
    string Label,
    short? PositionInBox);

public sealed record AssignStorageLocationRequest(
    Guid BinderId,
    string OwnerType,          // "shop_stock" | "user_collection"
    Guid OwnerId,
    short? SlotPosition);

public sealed record ConfirmMovementRequest(bool Confirm);
