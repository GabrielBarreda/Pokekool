namespace Pokekool.Application.DataCollection;

public interface ICardPriceProvider : IDataProvider
{
    /// <summary>cardIdentifier = TcgId ("base1-4") ou nom normalisé selon le provider.</summary>
    Task<CardPriceData?> GetPriceAsync(string cardIdentifier, CancellationToken ct = default);

    Task<IReadOnlyList<CardPriceData>> GetPricesBatchAsync(
        IEnumerable<string> cardIdentifiers, CancellationToken ct = default);
}
