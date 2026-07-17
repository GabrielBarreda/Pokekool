using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.ToggleShopProductAvailability;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>Met un produit du présentoir en vente ou hors vente. REQUIERT confirmation explicite.</summary>
public sealed class ToggleProductAvailabilityActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "toggle_shop_product_availability";
    public string Description =>
        "Met un produit du présentoir en vente (isAvailable=true) ou hors vente (false). REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            productId = new { type = "string", description = "UUID du produit du présentoir" },
            isAvailable = new { type = "boolean", description = "true = en vente, false = retiré de la vente" }
        },
        required = new[] { "productId", "isAvailable" }
    };

    public ToggleProductAvailabilityActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "productId")
                 ?? throw new ArgumentException("productId invalide.");
        var available = ChatArg.Bool(args, "isAvailable", true);
        var result = await _mediator.Send(new ToggleShopProductAvailabilityCommand(id, available), ct);
        return $"✅ Produit {result.Id} — {(result.IsAvailable ? "en vente" : "hors vente")}";
    }
}
