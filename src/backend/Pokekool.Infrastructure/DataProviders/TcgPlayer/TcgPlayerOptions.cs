namespace Pokekool.Infrastructure.DataProviders.TcgPlayer;

public sealed class TcgPlayerOptions
{
    public const string SectionName = "TcgPlayer";

    public string BaseUrl { get; set; } = "https://api.tcgplayer.com/v1.37.0";

    public string TokenUrl { get; set; } = "https://api.tcgplayer.com/token";

    /// <summary>Category ID Pokémon dans TCGPlayer.</summary>
    public int CategoryId { get; set; } = 3;
}
