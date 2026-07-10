using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Storage;

/// <summary>
/// Job asynchrone de compilation d'un fichier Blender (.blend) vers un modèle 3D (.glb).
/// Consommé par <c>BlenderCompileBackgroundService</c>.
///
/// <para>Généricité (cf. feedback clean-archi 2026-07-08) : <see cref="TargetType"/>/<see cref="TargetId"/>
/// restent polymorphes bien que seul <c>room</c> existe aujourd'hui — permet d'ajouter un modèle 3D
/// par carton (<c>box</c>) plus tard sans nouvelle table.</para>
/// </summary>
public sealed class StorageModelCompileJob : Entity
{
    public const string TargetRoom = "room";

    /// <summary>'room' (seul cas actuel, gardé générique).</summary>
    public string TargetType { get; private set; } = TargetRoom;
    public Guid TargetId { get; private set; }

    public CompileJobStatus Status { get; private set; } = CompileJobStatus.Queued;

    public string BlendFilePath { get; private set; } = string.Empty;
    public string? GlbOutputPath { get; private set; }
    public string? ErrorMessage { get; private set; }

    public Guid RequestedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private StorageModelCompileJob() { }

    /// <summary>
    /// Crée un job pour une salle. <paramref name="id"/> est fourni explicitement afin que
    /// l'appelant puisse nommer le fichier .blend de façon déterministe avant la persistance.
    /// </summary>
    public static StorageModelCompileJob CreateForRoom(Guid id, Guid roomId, string blendFilePath, Guid requestedBy)
    {
        if (string.IsNullOrWhiteSpace(blendFilePath))
            throw new DomainException("storage.compile.blend_required", "Chemin du fichier .blend requis.");

        return new StorageModelCompileJob
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id,
            TargetType = TargetRoom,
            TargetId = roomId,
            Status = CompileJobStatus.Queued,
            BlendFilePath = blendFilePath,
            RequestedBy = requestedBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkRunning()
    {
        Status = CompileJobStatus.Running;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkSucceeded(string glbOutputPath)
    {
        Status = CompileJobStatus.Succeeded;
        GlbOutputPath = glbOutputPath;
        ErrorMessage = null;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = CompileJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
