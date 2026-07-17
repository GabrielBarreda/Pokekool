using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Queries;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>Liste les salles de stockage 3D de la boutique (lecture seule).</summary>
public sealed class ListStorageRoomsTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "list_storage_rooms";
    public string Description => "Liste les salles de stockage physique de la boutique (avec leur ID).";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new { }
    };

    public ListStorageRoomsTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var rooms = await _mediator.Send(new GetStorageRoomsQuery(), ct);
        if (rooms.Count == 0)
            return "Aucune salle de stockage configurée.";

        var lines = rooms.Select(r => $"- [{r.Id}] {r.Name}");
        return $"Salles de stockage ({rooms.Count}) :\n" + string.Join("\n", lines);
    }
}
