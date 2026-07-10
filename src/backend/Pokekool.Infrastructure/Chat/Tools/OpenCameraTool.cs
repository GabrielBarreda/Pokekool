using Pokekool.Application.Chat;

namespace Pokekool.Infrastructure.Chat.Tools;

public sealed class OpenCameraTool : IChatTool
{
    public string Name => "open_camera";
    public string Description => "Ouvre la caméra pour identifier un produit (carte ou UPC scellé). Redirige vers le scanner.";
    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            purpose = new { type = "string", description = "card | upc" }
        },
        required = new[] { "purpose" }
    };

    public Task<string> ExecuteAsync(IDictionary<string, object?> args, ChatSession session, CancellationToken ct)
    {
        var purpose = args.GetValueOrDefault("purpose")?.ToString() ?? "card";
        // Signal spécial lu par le ChatOrchestrator → directive redirect au frontend.
        return Task.FromResult($"__redirect_camera:{purpose}");
    }
}
