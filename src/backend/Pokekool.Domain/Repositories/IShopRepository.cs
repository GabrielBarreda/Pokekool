using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Domain.Repositories;

public interface IShopRepository
{
    /// <summary>
    /// Retourne l'unique boutique (singleton). En crée une vide et la persiste
    /// si aucune n'existe encore.
    /// </summary>
    Task<Shop> GetOrCreateAsync(CancellationToken ct = default);
}
