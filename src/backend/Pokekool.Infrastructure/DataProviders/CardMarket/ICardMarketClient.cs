namespace Pokekool.Infrastructure.DataProviders.CardMarket;

public interface ICardMarketClient
{
    Task<List<CmExpansion>> GetExpansionsAsync(CancellationToken ct = default);
    Task<List<CmSingle>> GetSinglesAsync(int idExpansion, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}
