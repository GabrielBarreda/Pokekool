namespace Pokekool.Infrastructure.DataProviders.TcgPlayer;

internal interface ITcgPlayerClient
{
    /// <summary>Retourne tous les sets Pokémon (category 3) depuis TCGPlayer.</summary>
    Task<List<TcgPlayerSet>> GetSetsAsync(CancellationToken ct = default);

    /// <summary>Retourne les produits (cartes) d'un groupe (set).</summary>
    Task<List<TcgPlayerProduct>> GetProductsAsync(int groupId, CancellationToken ct = default);

    /// <summary>
    /// Retourne les prix de plusieurs produits en une seule requête (max 250 IDs).
    /// Retourne les prix de type "Normal" par défaut.
    /// </summary>
    Task<List<TcgPlayerPrice>> GetPricesAsync(
        IEnumerable<int> productIds, CancellationToken ct = default);
}
