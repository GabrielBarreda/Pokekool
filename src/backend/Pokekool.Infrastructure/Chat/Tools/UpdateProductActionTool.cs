using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.UpdateShopProduct;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Modifie un produit existant du présentoir (prix, quantité, disponibilité, état, description).
/// Charge le produit puis n'écrase que les champs fournis (merge). REQUIERT confirmation.
/// </summary>
public sealed class UpdateProductActionTool : IChatTool
{
    private readonly IMediator _mediator;
    private readonly IShopProductRepository _repo;

    public string Name => "update_shop_product";
    public string Description =>
        "Modifie un produit existant du présentoir (prix, quantité, disponibilité, état, description). " +
        "Seuls les champs fournis sont modifiés. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            productId = new { type = "string", description = "UUID du produit du présentoir" },
            price = new { type = "number", description = "Nouveau prix de vente" },
            currency = new { type = "string", description = "EUR (défaut)" },
            quantity = new { type = "integer" },
            isAvailable = new { type = "boolean" },
            condition = new { type = "string", description = "État : NM, LP, MP, HP, DMG" },
            description = new { type = "string" }
        },
        required = new[] { "productId" }
    };

    public UpdateProductActionTool(IMediator mediator, IShopProductRepository repo)
    {
        _mediator = mediator;
        _repo = repo;
    }

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "productId")
                 ?? throw new ArgumentException("productId invalide.");

        var current = await _repo.GetByIdAsync(id, ct)
                      ?? throw new ArgumentException($"Produit {id} introuvable.");

        var cmd = new UpdateShopProductCommand(
            Id: id,
            Type: current.Type.ToString(),
            CardId: current.CardId,
            ProductName: current.ProductName,
            Language: current.Language,
            Condition: ChatArg.String(args, "condition") ?? current.Condition,
            GradingCompany: current.GradingCompany,
            GradeValue: current.GradeValue,
            Price: ChatArg.Decimal(args, "price") ?? current.Price,
            Currency: ChatArg.String(args, "currency") ?? current.Currency,
            Quantity: args.ContainsKey("quantity") ? ChatArg.Int(args, "quantity", current.Quantity) : current.Quantity,
            IsAvailable: args.ContainsKey("isAvailable") ? ChatArg.Bool(args, "isAvailable", current.IsAvailable) : current.IsAvailable,
            Description: ChatArg.String(args, "description") ?? current.Description,
            PhotoUrl: current.PhotoUrl);

        var result = await _mediator.Send(cmd, ct);
        return $"✅ Produit mis à jour — ID: {result.Id}, '{result.ProductName}' à {result.Price} {result.Currency} · dispo:{result.IsAvailable}";
    }
}
