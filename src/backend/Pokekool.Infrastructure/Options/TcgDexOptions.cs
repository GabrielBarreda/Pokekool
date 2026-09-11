namespace Pokekool.Infrastructure.Options;

public sealed class TcgDexOptions
{
    public const string SectionName = "TcgDexApi";
    public string BaseUrl { get; init; } = "https://api.tcgdex.net/v2";
}
