using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.DeleteShopProduct;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>Retire définitivement un produit du présentoir. REQUIERT confirmation explicite.</summary>
public sealed class DeleteProductActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "delete_shop_product";
    public string Description => "Retire définitivement un produit du présentoir. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            productId = new { type = "string", description = "UUID du produit du présentoir à supprimer" }
        },
        required = new[] { "productId" }
    };

    public DeleteProductActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "productId")
                 ?? throw new ArgumentException("productId invalide.");
        await _mediator.Send(new DeleteShopProductCommand(id), ct);
        return $"✅ Produit retiré du présentoir — ID: {id}";
    }
}
