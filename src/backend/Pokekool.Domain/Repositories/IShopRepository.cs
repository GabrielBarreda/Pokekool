using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Domain.Repositories;

public interface IShopRepository
{
    /// <summary>
    /// Retourne l'unique boutique (singleton). En crée une vide et la persiste
    /// si aucune n'existe encore.
    /// </summary>
    Task<Shop> GetOrCreateAsync(CancellationToken ct = default);

    /// <summary>Retourne la boutique par son id (null si absente). Utilisé par le pipeline Blender PLV.</summary>
    Task<Shop?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
