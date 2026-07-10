namespace Pokekool.Application.Storage;

/// <summary>
/// Abstraction du stockage disque des fichiers Blender (garde la couche Application
/// libre de toute dépendance IO / configuration — cf. feedback clean-archi).
/// Implémentée dans Infrastructure (<c>BlenderFileStore</c>).
/// </summary>
public interface IBlenderFileStore
{
    /// <summary>Persiste le .blend uploadé et retourne son chemin absolu sur disque.</summary>
    Task<string> SaveBlendAsync(Guid jobId, Stream content, string fileName, CancellationToken ct);

    /// <summary>Chemin absolu du .glb à produire pour ce job.</summary>
    string GetGlbOutputPath(Guid jobId);

    /// <summary>URL publique (servie par wwwroot) correspondant à un .glb produit.</summary>
    string ToPublicUrl(string glbOutputPath);
}
