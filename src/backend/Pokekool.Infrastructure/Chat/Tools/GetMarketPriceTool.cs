using Pokekool.Application.Chat;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class GetMarketPriceTool : IChatTool
{
    private readonly IMarketPriceResearcher _researcher;

    public string Name => "get_market_price";
    public string Description => "Cherche le prix de marché actuel d'une carte ou d'un produit scellé (eBay ventes récentes).";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            productName = new { type = "string", description = "Nom exact du produit (ex: 'Charizard Base Set PSA 10', 'Booster Box SV151')" }
        },
        required = new[] { "productName" }
    };

    public GetMarketPriceTool(IMarketPriceResearcher researcher) => _researcher = researcher;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var name = args.GetValueOrDefault("productName")?.ToString() ?? "";
        var result = await _researcher.SearchEbayRecentSalesAsync(name, ct);

        if (result is null)
            return $"Impossible de récupérer le prix marché pour '{name}' (eBay indisponible).";

        return $"Prix marché actuel pour '{result.ProductName}' ({result.SampleCount} ventes récentes) :\n" +
               $"  Moyenne : {result.AveragePrice:F2} {result.Currency}\n" +
               $"  Fourchette : {result.MinPrice:F2} – {result.MaxPrice:F2} {result.Currency}";
    }
}
