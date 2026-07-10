using Pokekool.Application.Chat;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class SearchProductsTool : IChatTool
{
    private readonly IShopProductRepository _repo;

    public string Name => "search_shop_products";
    public string Description => "Cherche des articles sur le présentoir (produits en vente).";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            name = new { type = "string", description = "Nom du produit" },
            isAvailable = new { type = "boolean", description = "Filtrer sur disponibilité" }
        },
        required = new[] { "name" }
    };

    public SearchProductsTool(IShopProductRepository repo) => _repo = repo;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var name = args.GetValueOrDefault("name")?.ToString() ?? "";
        bool? avail = args.TryGetValue("isAvailable", out var a) && a is not null
            ? Convert.ToBoolean(a)
            : null;

        var (items, total) = await _repo.SearchAsync(type: null, isAvailable: avail, search: name, page: 1, pageSize: 10, ct);

        if (items.Count == 0)
            return $"Aucun produit trouvé sur le présentoir pour '{name}'.";

        var lines = items.Select(p =>
            $"- [{p.Id}] {p.ProductName ?? p.CardId?.ToString() ?? "?"} · {p.Price} {p.Currency} · dispo:{p.IsAvailable} · qty:{p.Quantity}");
        return $"Présentoir '{name}' ({total} résultat(s)) :\n" + string.Join("\n", lines);
    }
}
