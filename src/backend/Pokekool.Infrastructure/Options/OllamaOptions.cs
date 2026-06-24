namespace Pokekool.Infrastructure.Options;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "qwen2.5:7b";
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxTokens { get; init; } = 1024;
}
