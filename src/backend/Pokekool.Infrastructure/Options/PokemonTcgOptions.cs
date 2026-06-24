namespace Pokekool.Infrastructure.Options;

public sealed class PokemonTcgOptions
{
    public const string SectionName = "PokemonTcgApi";
    public string BaseUrl { get; init; } = "https://api.pokemontcg.io/v2";
    public string ApiKey { get; init; } = "";
}
