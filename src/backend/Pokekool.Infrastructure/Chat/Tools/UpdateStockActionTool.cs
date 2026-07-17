using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.UpdateShopStock;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Modifie un article existant du stock boutique (quantités, prix d'achat, état, notes).
/// Charge l'article puis n'écrase que les champs fournis (merge). REQUIERT confirmation.
/// </summary>
public sealed class UpdateStockActionTool : IChatTool
{
    private readonly IMediator _mediator;
    private readonly IShopStockRepository _repo;

    public string Name => "update_stock_entry";
    public string Description =>
        "Modifie un article existant du stock boutique (quantité, prix d'achat, état, notes). " +
        "Seuls les champs fournis sont modifiés. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            stockId = new { type = "string", description = "UUID de l'article de stock à modifier" },
            productName = new { type = "string" },
            condition = new { type = "string", description = "État : NM, LP, MP, HP, DMG" },
            quantityTotal = new { type = "integer" },
            quantityAvailable = new { type = "integer" },
            quantityReserved = new { type = "integer" },
            purchasePrice = new { type = "number" },
            notes = new { type = "string" }
        },
        required = new[] { "stockId" }
    };

    public UpdateStockActionTool(IMediator mediator, IShopStockRepository repo)
    {
        _mediator = mediator;
        _repo = repo;
    }

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "stockId")
                 ?? throw new ArgumentException("stockId invalide.");

        var current = await _repo.GetByIdAsync(id, ct)
                      ?? throw new ArgumentException($"Article de stock {id} introuvable.");

        var cmd = new UpdateShopStockCommand(
            Id: id,
            CardId: current.CardId,
            ProductName: ChatArg.String(args, "productName") ?? current.ProductName,
            Type: current.Type.ToString(),
            Language: current.Language,
            Condition: ChatArg.String(args, "condition") ?? current.Condition,
            GradingCompany: current.GradingCompany,
            GradeValue: current.GradeValue,
            QuantityTotal: args.ContainsKey("quantityTotal") ? ChatArg.Int(args, "quantityTotal", current.QuantityTotal) : current.QuantityTotal,
            QuantityAvailable: args.ContainsKey("quantityAvailable") ? ChatArg.Int(args, "quantityAvailable", current.QuantityAvailable) : current.QuantityAvailable,
            QuantityReserved: args.ContainsKey("quantityReserved") ? ChatArg.Int(args, "quantityReserved", current.QuantityReserved) : current.QuantityReserved,
            PurchasePrice: ChatArg.Decimal(args, "purchasePrice") ?? current.PurchasePrice,
            PurchaseCurrency: current.PurchaseCurrency ?? (ChatArg.Decimal(args, "purchasePrice").HasValue ? "EUR" : null),
            Notes: ChatArg.String(args, "notes") ?? current.Notes);

        var result = await _mediator.Send(cmd, ct);
        return $"✅ Stock mis à jour — ID: {result.Id}, '{result.ProductName}' · qty:{result.QuantityAvailable}/{result.QuantityTotal}";
    }
}
