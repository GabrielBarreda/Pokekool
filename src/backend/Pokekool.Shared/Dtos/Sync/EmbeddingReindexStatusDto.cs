namespace Pokekool.Shared.Dtos.Sync;

/// <summary>État de la réindexation des embeddings visuels DINOv2 (voir EmbeddingReindexState).</summary>
public sealed record EmbeddingReindexStatusDto(
    string    Status,          // "idle" | "running" | "completed"
    int       TotalItems,
    int       ItemsProcessed,
    int       ItemsFailed,
    int       Remaining,
    DateTime? StartedAt,
    DateTime? CompletedAt);
