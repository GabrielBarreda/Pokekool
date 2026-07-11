using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.CreateShopStock;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class CreateStockActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "create_stock_entry";
    public string Description => "Ajoute un ou plusieurs articles dans le stock boutique. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            productName = new { type = "string" },
            cardId = new { type = "string", description = "UUID de la carte (si applicable)" },
            type = new { type = "string", description = "CardRaw | CardGraded | SealedSet | SealedUpc" },
            condition     = new { type = "string", description = "État : NM (Neuf), LP (Très bon), MP (Bon), HP (Usé), DMG (Abîmé)" },
            gradingCompany = new { type = "string", description = "PSA | BGS | CGC | null" },
            gradeValue = new { type = "number" },
            quantityTotal = new { type = "integer" },
            purchasePrice = new { type = "number" },
            notes = new { type = "string" }
        },
        required = new[] { "productName", "type", "quantityTotal" }
    };

    public CreateStockActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var qty = ChatArg.Int(args, "quantityTotal");
        var purchasePrice = ChatArg.Decimal(args, "purchasePrice");

        var cmd = new CreateShopStockCommand(
            CardId: ChatArg.Guid(args, "cardId"),
            ProductName: ChatArg.String(args, "productName"),
            Type: ChatArg.String(args, "type") ?? "CardRaw",
            Language: ChatArg.String(args, "language") ?? "EN",
            Condition: ChatArg.String(args, "condition"),
            GradingCompany: ChatArg.String(args, "gradingCompany"),
            GradeValue: ChatArg.Decimal(args, "gradeValue"),
            QuantityTotal: qty,
            QuantityAvailable: qty,
            QuantityReserved: 0,
            PurchasePrice: purchasePrice,
            PurchaseCurrency: purchasePrice.HasValue ? "EUR" : null,
            Notes: ChatArg.String(args, "notes"));

        var result = await _mediator.Send(cmd, ct);
        return $"✅ Stock créé — ID: {result.Id}, '{result.ProductName}' x{result.QuantityTotal}";
    }
}
