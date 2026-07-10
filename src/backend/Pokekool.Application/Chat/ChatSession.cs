using Pokekool.Shared.Dtos.Chat;

namespace Pokekool.Application.Chat;

/// <summary>État de la conversation, stocké en Redis (TTL configurable).</summary>
public sealed class ChatSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public Guid UserId { get; init; }
    public List<OllamaChatMessage> History { get; init; } = new();
    public ChatPendingAction? PendingAction { get; set; }

    public void AddUserMessage(string content)
        => History.Add(new OllamaChatMessage("user", content));

    public void AddAssistantMessage(string content)
        => History.Add(new OllamaChatMessage("assistant", content));

    public void AddToolResult(string toolName, string result)
        => History.Add(new OllamaChatMessage("tool", $"[{toolName}] {result}"));

    /// <summary>Conserve au plus les N derniers messages pour limiter le contexte LLM.</summary>
    public IReadOnlyList<OllamaChatMessage> GetRecentHistory(int maxMessages = 20)
        => History.TakeLast(maxMessages).ToList();
}
