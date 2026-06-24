using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface ICardMarketPriceRepository
{
    Task<IReadOnlyList<CardMarketPrice>> GetByCardIdAsync(Guid cardId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<CardMarketPrice>>> GetByCardIdsAsync(
        IEnumerable<Guid> cardIds, CancellationToken ct = default);
    Task UpsertAsync(CardMarketPrice price, CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<CardMarketPrice> prices, CancellationToken ct = default);

    /// <summary>
    /// Retourne les IDs de toutes les cartes qui n'ont pas de prix récent pour <paramref name="source"/>.
    /// "Périmé" = absent OU dernière mise à jour antérieure à <paramref name="olderThanHours"/> heures.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetStaleCardIdsAsync(
        string source,
        int olderThanHours,
        CancellationToken ct = default);
}
