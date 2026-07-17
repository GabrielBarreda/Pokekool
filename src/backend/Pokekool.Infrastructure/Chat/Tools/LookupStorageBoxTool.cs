using MediatR;
using Pokekool.Application.Chat;
using Pokekool.Application.Storage.Queries;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Localise un carton par code-barre ou code interne et retourne son contenu attendu
/// (classeurs + emplacements). C'est le "scan théorique" en lecture seule.
/// </summary>
public sealed class LookupStorageBoxTool : IChatTool
{
    private readonly IMediator _mediator;

    public string Name => "lookup_storage_box";
    public string Description =>
        "Localise un carton par son code-barre OU code interne et affiche son contenu " +
        "attendu (classeurs et emplacements). Utile pour retrouver où est rangé un lot.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            code = new { type = "string", description = "Code-barre ou code interne du carton" }
        },
        required = new[] { "code" }
    };

    public LookupStorageBoxTool(IMediator mediator) => _mediator = mediator;

    public async Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var code = ChatArg.String(args, "code");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("code requis.");

        var lookup = await _mediator.Send(new GetBoxContentLookupQuery(code), ct);
        var box = lookup.Box;

        var header = $"📦 Carton [{box.Id}] {box.InternalCode ?? box.Barcode ?? "?"}" +
                     $"{(box.FurnitureId is null ? " · au sol" : $" · meuble {box.FurnitureId} niv.{box.LevelIndex}")}";

        if (lookup.Binders.Count == 0)
            return header + "\n(aucun classeur enregistré)";

        var lines = lookup.Binders.Select(b =>
            $"- Classeur [{b.Binder.Id}] {b.Binder.Label} · {b.Locations.Count} emplacement(s)");
        return header + $"\nContenu ({lookup.Binders.Count} classeur(s)) :\n" + string.Join("\n", lines);
    }
}
