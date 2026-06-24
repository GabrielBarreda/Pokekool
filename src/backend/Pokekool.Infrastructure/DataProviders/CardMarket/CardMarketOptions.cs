namespace Pokekool.Infrastructure.DataProviders.CardMarket;

public sealed class CardMarketOptions
{
    public const string SectionName = "CardMarket";

    public string BaseUrl { get; set; } = "https://api.cardmarket.com/ws/v2.0/output.json";

    /// <summary>ID du jeu Pokémon dans l'API CardMarket.</summary>
    public int PokemonGameId { get; set; } = 3;
}
