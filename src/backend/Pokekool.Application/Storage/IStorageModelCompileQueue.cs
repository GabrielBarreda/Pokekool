namespace Pokekool.Application.Storage;

/// <summary>
/// File d'attente asynchrone des jobs de compilation Blender. Implémentation par défaut :
/// <c>InMemoryCompileQueue</c> (System.Threading.Channels), consommée par
/// <c>BlenderCompileBackgroundService</c>.
/// </summary>
public interface IStorageModelCompileQueue
{
    Task EnqueueAsync(Guid jobId, CancellationToken ct);
}
