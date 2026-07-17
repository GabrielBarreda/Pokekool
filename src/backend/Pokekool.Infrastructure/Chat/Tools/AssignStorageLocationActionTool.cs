using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Commands;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Range un article de stock dans un classeur (ou le déplace s'il est déjà rangé ailleurs).
/// REQUIERT confirmation explicite.
/// </summary>
public sealed class AssignStorageLocationActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "assign_storage_location";
    public string Description =>
        "Range un article (stock boutique par défaut) dans un classeur de stockage, " +
        "ou le déplace s'il est déjà rangé ailleurs. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            binderId = new { type = "string", description = "UUID du classeur cible" },
            ownerId = new { type = "string", description = "UUID de l'item à ranger (id de l'article de stock)" },
            ownerType = new { type = "string", description = "shop_stock (défaut) | user_collection" },
            slotPosition = new { type = "integer", description = "Position dans le classeur (optionnel)" }
        },
        required = new[] { "binderId", "ownerId" }
    };

    public AssignStorageLocationActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var binderId = ChatArg.Guid(args, "binderId")
                       ?? throw new ArgumentException("binderId invalide.");
        var ownerId = ChatArg.Guid(args, "ownerId")
                      ?? throw new ArgumentException("ownerId invalide.");
        var ownerType = ChatArg.String(args, "ownerType") ?? "shop_stock";
        short? slot = args.ContainsKey("slotPosition") ? (short)ChatArg.Int(args, "slotPosition") : null;

        var result = await _mediator.Send(
            new AssignStorageLocationCommand(binderId, ownerType, ownerId, slot), ct);
        return $"✅ Rangé — item {result.OwnerId} dans classeur {result.BinderId}" +
               $"{(result.SlotPosition is null ? "" : $" (position {result.SlotPosition})")}";
    }
}
