namespace Pokekool.Application.DataCollection;

public interface IDataProvider
{
    string Name { get; }
    ProviderCapability Capabilities { get; }
}

[Flags]
public enum ProviderCapability
{
    None         = 0,
    CardMetadata = 1 << 0,
    MarketPrices = 1 << 1,
    GradedPrices = 1 << 2,
    Population   = 1 << 3,
    SalesHistory = 1 << 4,
}
