namespace Pokekool.Application.DataCollection;

public interface IPopulationProvider : IDataProvider
{
    Task<CardPopulationData?> GetPopulationAsync(
        string cardName, string setName, CancellationToken ct = default);
}
