using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.CreateShopProduct;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class CreateProductActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "create_shop_product";
    public string Description => "Met en vente un article sur le présentoir. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            productName = new { type = "string" },
            cardId = new { type = "string" },
            type = new { type = "string", description = "CardRaw | CardGraded | SealedSet | SealedUpc" },
            condition = new { type = "string" },
            gradingCompany = new { type = "string" },
            gradeValue = new { type = "number" },
            price = new { type = "number", description = "Prix de vente" },
            currency = new { type = "string", description = "EUR (défaut)" },
            quantity = new { type = "integer" },
            isAvailable = new { type = "boolean" },
            description = new { type = "string" }
        },
        required = new[] { "productName", "type", "price", "quantity" }
    };

    public CreateProductActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var cmd = new CreateShopProductCommand(
            Type: ChatArg.String(args, "type") ?? "CardRaw",
            CardId: ChatArg.Guid(args, "cardId"),
            ProductName: ChatArg.String(args, "productName"),
            Condition: ChatArg.String(args, "condition"),
            GradingCompany: ChatArg.String(args, "gradingCompany"),
            GradeValue: ChatArg.Decimal(args, "gradeValue"),
            Price: ChatArg.Decimal(args, "price") ?? 0m,
            Currency: ChatArg.String(args, "currency") ?? "EUR",
            Quantity: ChatArg.Int(args, "quantity"),
            IsAvailable: ChatArg.Bool(args, "isAvailable", true),
            Description: ChatArg.String(args, "description"),
            PhotoUrl: null);

        var result = await _mediator.Send(cmd, ct);
        return $"✅ Produit créé — ID: {result.Id}, '{result.ProductName}' à {result.Price} {result.Currency}";
    }
}
