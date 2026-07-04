using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface ICardGradedPriceRepository
{
    Task<IReadOnlyList<CardGradedPrice>> GetByCardIdAsync(
        Guid cardId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<CardGradedPrice>>> GetByCardIdsAsync(
        IEnumerable<Guid> cardIds, CancellationToken ct = default);

    Task UpsertBatchAsync(
        IEnumerable<CardGradedPrice> prices, CancellationToken ct = default);
}
