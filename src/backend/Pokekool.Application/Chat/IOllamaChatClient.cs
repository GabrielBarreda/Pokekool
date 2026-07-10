namespace Pokekool.Application.Chat;

public interface IOllamaChatClient
{
    Task<OllamaChatResponse> SendAsync(
        IReadOnlyList<OllamaChatMessage> messages,
        IReadOnlyList<OllamaToolDefinition> tools,
        CancellationToken ct = default);
}

public sealed record OllamaChatMessage(string Role, string? Content, string? ToolCallId = null);

public sealed record OllamaToolDefinition(string Name, string Description, object ParametersSchema);

public sealed record OllamaChatResponse(
    string? Content,
    IReadOnlyList<OllamaToolCall> ToolCalls
);

public sealed record OllamaToolCall(string FunctionName, IDictionary<string, object?> Arguments);
