using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;

namespace Pokekool.Application.Storage;

/// <summary>Parsing tolérant des enums Stockage depuis les chaînes reçues côté API.</summary>
internal static class StorageEnumMap
{
    public static LabelOrigin ParseLabelOrigin(string? value)
        => Enum.TryParse<LabelOrigin>(value, ignoreCase: true, out var result)
            ? result
            : throw new DomainException("storage.box.invalid_label_origin",
                $"Origine d'étiquette invalide : '{value}'. Attendu : Supplier ou Internal.");

    public static LabelOrigin? ParseLabelOriginOrNull(string? value)
        => Enum.TryParse<LabelOrigin>(value, ignoreCase: true, out var result) ? result : null;

    public static FurnitureType ParseFurnitureType(string? value)
        => Enum.TryParse<FurnitureType>(value, ignoreCase: true, out var result)
            ? result
            : throw new DomainException("storage.furniture.invalid_type",
                $"Type de meuble invalide : '{value}'. Attendu : ShelfClassic6 ou ShelfWall.");
}
