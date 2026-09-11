namespace Pokekool.Infrastructure.Services;

public enum EmbeddingReindexStatus { Idle, Running, Completed }

/// <summary>
/// État courant de la réindexation des embeddings visuels DINOv2, mis à jour par
/// <see cref="EmbeddingReindexHostedService"/> et lu par l'endpoint de statut ainsi que par
/// le flux SSE de progression (voir ISyncProgressReporter). Singleton en mémoire, symétrique
/// à SyncScheduleState — pas de persistance nécessaire (recalculé à chaque démarrage API).
/// </summary>
public sealed class EmbeddingReindexState
{
    public EmbeddingReindexStatus Status         { get; set; } = EmbeddingReindexStatus.Idle;
    public int                    TotalItems     { get; set; }
    public int                    ItemsProcessed { get; set; }
    public int                    ItemsFailed    { get; set; }
    public DateTime?              StartedAt      { get; set; }
    public DateTime?              CompletedAt    { get; set; }

    public int Remaining => Math.Max(0, TotalItems - ItemsProcessed - ItemsFailed);
}
