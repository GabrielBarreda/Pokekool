namespace Pokekool.Infrastructure.Options;

public sealed class OllamaVisionOptions
{
    public const string SectionName = "OllamaVision";

    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "llava:13b";
    public int TimeoutSeconds { get; init; } = 120;
}
