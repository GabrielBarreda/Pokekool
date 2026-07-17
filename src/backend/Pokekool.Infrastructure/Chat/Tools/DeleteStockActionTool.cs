using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Shop.Commands.DeleteShopStock;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>Supprime un article du stock boutique. REQUIERT confirmation explicite.</summary>
public sealed class DeleteStockActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "delete_stock_entry";
    public string Description => "Supprime définitivement un article du stock boutique. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            stockId = new { type = "string", description = "UUID de l'article de stock à supprimer" }
        },
        required = new[] { "stockId" }
    };

    public DeleteStockActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var id = ChatArg.Guid(args, "stockId")
                 ?? throw new ArgumentException("stockId invalide.");
        await _mediator.Send(new DeleteShopStockCommand(id), ct);
        return $"✅ Article de stock supprimé — ID: {id}";
    }
}
