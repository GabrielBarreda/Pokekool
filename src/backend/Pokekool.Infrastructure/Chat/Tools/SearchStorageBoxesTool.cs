using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Queries;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>Liste les cartons d'une salle de stockage donnée (lecture seule).</summary>
public sealed class SearchStorageBoxesTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "list_storage_boxes";
    public string Description =>
        "Liste les cartons d'une salle de stockage (par roomId). " +
        "Utilise d'abord list_storage_rooms pour obtenir le roomId.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            roomId = new { type = "string", description = "UUID de la salle de stockage" },
            labelOrigin = new { type = "string", description = "Filtre optionnel sur l'origine de l'étiquette (ex: Supplier, Internal)" }
        },
        required = new[] { "roomId" }
    };

    public SearchStorageBoxesTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var roomId = ChatArg.Guid(args, "roomId")
                     ?? throw new ArgumentException("roomId invalide.");
        var labelOrigin = ChatArg.String(args, "labelOrigin");

        var boxes = await _mediator.Send(new GetStorageBoxesQuery(roomId, labelOrigin), ct);
        if (boxes.Count == 0)
            return "Aucun carton dans cette salle.";

        var lines = boxes.Select(b =>
            $"- [{b.Id}] {b.InternalCode ?? b.Barcode ?? "?"} · {b.Size ?? "?"}{(b.FurnitureId is null ? " · au sol" : $" · meuble {b.FurnitureId} niv.{b.LevelIndex}")}");
        return $"Cartons ({boxes.Count}) :\n" + string.Join("\n", lines);
    }
}
