namespace Pokekool.Infrastructure.Options;

public sealed class CardEmbeddingOptions
{
    public const string SectionName = "CardEmbedding";

    public string BaseUrl { get; init; } = "http://localhost:8500";
    public int TimeoutSeconds { get; init; } = 30;
}
