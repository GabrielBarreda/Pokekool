using Pokekool.Domain.Entities.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage;

/// <summary>Mappings entités -> DTOs du module Stockage Physique.</summary>
internal static class StorageMapper
{
    public static StorageRoomDto ToDto(StorageRoom r) => new(
        r.Id, r.ShopId, r.Name, r.FloorPlanUrl, r.Model3dUrl, r.CreatedAt, r.UpdatedAt);

    public static StorageBoxDto ToDto(StorageBox b) => new(
        b.Id, b.RoomId, b.LabelOrigin.ToString(), b.Barcode, b.InternalCode,
        b.SupplierName, b.Size, b.Position3dJson, b.Notes, b.CreatedAt, b.UpdatedAt);

    public static StorageBinderDto ToDto(StorageBinder b) => new(
        b.Id, b.BoxId, b.Label, b.PositionInBox, b.CreatedAt, b.UpdatedAt);

    public static StorageLocationDto ToDto(StorageLocation l) => new(
        l.Id, l.BinderId, l.OwnerType, l.OwnerId, l.SlotPosition, l.MovedAt);

    public static StorageMovementEventDto ToDto(StorageMovementEvent m) => new(
        m.Id, m.BoxId, m.DetectedAt, m.FromPosition3dJson, m.ToPosition3dJson,
        m.Status.ToString(), m.ConfirmedBy, m.ConfirmedAt, m.CreatedAt);

    public static StorageCompileJobDto ToDto(StorageModelCompileJob j) => new(
        j.Id, j.TargetType, j.TargetId, j.Status.ToString(), j.GlbOutputPath,
        j.ErrorMessage, j.CreatedAt, j.CompletedAt);
}
