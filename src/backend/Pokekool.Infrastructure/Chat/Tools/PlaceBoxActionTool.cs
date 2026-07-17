using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Commands;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Place un carton : sur un meuble (furnitureId + levelIndex) ou au sol (sans meuble).
/// REQUIERT confirmation explicite.
/// </summary>
public sealed class PlaceBoxActionTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "place_box";
    public string Description =>
        "Place ou déplace un carton : sur un meuble (furnitureId + levelIndex) ou au sol " +
        "(ne pas fournir furnitureId). REQUIERT confirmation explicite.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            boxId = new { type = "string", description = "UUID du carton" },
            furnitureId = new { type = "string", description = "UUID du meuble (omettre pour poser au sol)" },
            levelIndex = new { type = "integer", description = "Niveau/étagère du meuble (défaut 0)" }
        },
        required = new[] { "boxId" }
    };

    public PlaceBoxActionTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var boxId = ChatArg.Guid(args, "boxId")
                    ?? throw new ArgumentException("boxId invalide.");
        var furnitureId = ChatArg.Guid(args, "furnitureId");
        short? level = args.ContainsKey("levelIndex") ? (short)ChatArg.Int(args, "levelIndex") : null;

        var result = await _mediator.Send(
            new PlaceBoxCommand(boxId, furnitureId, level, Position3dJson: null), ct);
        return furnitureId is null
            ? $"✅ Carton {result.Id} posé au sol"
            : $"✅ Carton {result.Id} placé sur meuble {result.FurnitureId} niv.{result.LevelIndex}";
    }
}
