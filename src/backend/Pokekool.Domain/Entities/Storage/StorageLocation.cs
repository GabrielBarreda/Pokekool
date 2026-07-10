using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Rattache un item existant (stock boutique ou collection utilisateur) à un classeur physique.
/// Référence polymorphe (<see cref="OwnerType"/> + <see cref="OwnerId"/>) : pas de FK DB stricte,
/// seulement un index (owner_type, owner_id). N'altère aucune table existante.
/// </summary>
public sealed class StorageLocation : Entity
{
    public const string OwnerShopStock      = "shop_stock";
    public const string OwnerUserCollection = "user_collection";

    public Guid BinderId { get; private set; }

    /// <summary>'shop_stock' | 'user_collection'.</summary>
    public string OwnerType { get; private set; } = OwnerShopStock;

    /// <summary>Id de l'item possédé (ShopStock.Id ou UserCollection.Id).</summary>
    public Guid OwnerId { get; private set; }

    public short? SlotPosition { get; private set; }
    public DateTime MovedAt { get; private set; }

    private StorageLocation() { }

    public static StorageLocation Create(Guid binderId, string ownerType, Guid ownerId, short? slotPosition)
    {
        if (ownerType is not (OwnerShopStock or OwnerUserCollection))
            throw new DomainException("storage.location.invalid_owner_type",
                "OwnerType doit être 'shop_stock' ou 'user_collection'.");

        return new StorageLocation
        {
            Id = Guid.NewGuid(),
            BinderId = binderId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            SlotPosition = slotPosition,
            MovedAt = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Déplace la localisation vers un autre classeur / emplacement.</summary>
    public void MoveTo(Guid binderId, short? slotPosition)
    {
        BinderId = binderId;
        SlotPosition = slotPosition;
        MovedAt = DateTime.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
