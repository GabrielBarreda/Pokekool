namespace Pokekool.Domain.Repositories;

public sealed record RecentlyScrapedCardProjection(
    string   CardId,
    string   Name,
    string?  ImageSmall,
    string   SetName,
    string?  Rarity,
    decimal? PriceMarket,
    string?  PriceCurrency,
    string?  PriceSource,
    DateTime ScrapedAt
);

public interface IDashboardRepository
{
    /// <summary>
    /// Cartes dont le prix a été scrapé dans les dernières 24h,
    /// dédupliquées par carte (prix le plus récent retenu), limitées à 20.
    /// </summary>
    Task<IReadOnlyList<RecentlyScrapedCardProjection>> GetRecentlyScrapedCardsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Nombre d'utilisateurs distincts ayant au moins une carte possédée (IsOwned = true).
    /// </summary>
    Task<int> GetTotalUsersCollectingAsync(CancellationToken ct = default);
}
