namespace Pokekool.Application.DataCollection;

public interface ICardMetadataProvider : IDataProvider
{
    Task<IReadOnlyList<CardSetData>> GetAllSetsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CardData>> GetCardsBySetAsync(string setTcgId, CancellationToken ct = default);
}
