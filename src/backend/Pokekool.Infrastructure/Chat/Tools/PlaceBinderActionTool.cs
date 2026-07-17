using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Commands;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Place un classeur : dans un carton (boxId) ou sur un meuble (furnitureId + levelIndex).
/// REQUIERT confirmation explicite.
/// </summary>
public sealed class PlaceBinderActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "place_binder";
    public string Description =>
        "Place ou déplace un classeur : dans un carton (boxId) ou sur un meuble " +
        "(furnitureId + levelIndex). Fournir l'un OU l'autre. REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            binderId = new { type = "string", description = "UUID du classeur" },
            boxId = new { type = "string", description = "UUID du carton (si rangé dans un carton)" },
            positionInBox = new { type = "integer", description = "Position dans le carton (optionnel)" },
            furnitureId = new { type = "string", description = "UUID du meuble (si posé sur un meuble)" },
            levelIndex = new { type = "integer", description = "Niveau/étagère du meuble (défaut 0)" }
        },
        required = new[] { "binderId" }
    };

    public PlaceBinderActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var binderId = ChatArg.Guid(args, "binderId")
                       ?? throw new ArgumentException("binderId invalide.");
        var boxId = ChatArg.Guid(args, "boxId");
        var furnitureId = ChatArg.Guid(args, "furnitureId");
        short? positionInBox = args.ContainsKey("positionInBox") ? (short)ChatArg.Int(args, "positionInBox") : null;
        short? level = args.ContainsKey("levelIndex") ? (short)ChatArg.Int(args, "levelIndex") : null;

        var result = await _mediator.Send(
            new PlaceBinderCommand(binderId, boxId, positionInBox, furnitureId, level), ct);
        return result.FurnitureId is not null
            ? $"✅ Classeur {result.Id} placé sur meuble {result.FurnitureId} niv.{result.LevelIndex}"
            : $"✅ Classeur {result.Id} rangé dans carton {result.BoxId}";
    }
}
