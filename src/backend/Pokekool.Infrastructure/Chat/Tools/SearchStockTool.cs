using Pokekool.Application.Chat;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class SearchStockTool : IChatTool
{
    private readonly IShopStockRepository _repo;

    public string Name => "search_shop_stock";
    public string Description => "Cherche des articles dans le stock boutique par nom de carte ou de produit.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "Nom de la carte ou du produit (ex: 'Deoxys', 'Booster Box SV151')" },
            maxResults = new { type = "integer", description = "Nombre max de résultats (défaut: 10)" }
        },
        required = new[] { "name" }
    };

    public SearchStockTool(IShopStockRepository repo) => _repo = repo;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var name = args.GetValueOrDefault("name")?.ToString() ?? "";
        var max = ToInt(args.GetValueOrDefault("maxResults"), 10);

        var (items, total) = await _repo.SearchAsync(type: null, search: name, page: 1, pageSize: max, ct);

        if (items.Count == 0)
            return $"Aucun article trouvé dans le stock pour '{name}'.";

        var lines = items.Select(s =>
            $"- [{s.Id}] {s.ProductName ?? s.CardId?.ToString() ?? "?"} · {s.Condition ?? "?"} · qty:{s.QuantityAvailable}/{s.QuantityTotal}");
        return $"Stock trouvé pour '{name}' ({total} résultat(s)) :\n" + string.Join("\n", lines);
    }

    private static int ToInt(object? value, int fallback)
        => value switch
        {
            null => fallback,
            int i => i,
            long l => (int)l,
            _ => int.TryParse(value.ToString(), out var v) ? v : fallback
        };
}
