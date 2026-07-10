namespace Pokekool.Application.Chat;

public interface IMarketPriceResearcher
{
    Task<MarketPriceResult?> SearchEbayRecentSalesAsync(string productName, CancellationToken ct = default);
}

public sealed record MarketPriceResult(
    string ProductName,
    decimal AveragePrice,
    decimal MinPrice,
    decimal MaxPrice,
    int SampleCount,
    string Currency,
    DateTimeOffset RetrievedAt
);
