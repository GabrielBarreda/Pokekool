namespace Pokekool.Infrastructure.Options;

public sealed class OllamaChatOptions
{
    public const string SectionName = "OllamaChat";

    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "qwen2.5:7b";
    public int TimeoutSeconds { get; init; } = 120;
    public int MaxTokens { get; init; } = 2048;
    public int SessionTtlMinutes { get; init; } = 30;
}
