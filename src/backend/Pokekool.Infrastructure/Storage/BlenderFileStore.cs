using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pokekool.Application.Storage;
using Pokekool.Infrastructure.Options;

namespace Pokekool.Infrastructure.Storage;

/// <summary>
/// Stockage disque des fichiers Blender. Chemins dérivés de <see cref="BlenderOptions"/>,
/// résolus par rapport au ContentRoot de l'application (IHostEnvironment.ContentRootPath),
/// jamais par rapport au répertoire courant du process (instable selon IDE/service/conteneur).
/// </summary>
internal sealed class BlenderFileStore : IBlenderFileStore
{
    private readonly BlenderOptions _options;
    private readonly IHostEnvironment _environment;

    public BlenderFileStore(IOptions<BlenderOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    private string ResolvePath(string configuredPath) =>
        Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredPath));

    public async Task<string> SaveBlendAsync(Guid jobId, Stream content, string fileName, CancellationToken ct)
    {
        var dir = ResolvePath(_options.UploadDirectory);
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"{jobId}.blend");
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, ct);
        return path;
    }

    public string GetGlbOutputPath(Guid jobId)
    {
        var dir = ResolvePath(_options.OutputDirectory);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{jobId}.glb");
    }

    public string ToPublicUrl(string glbOutputPath)
    {
        var fileName = Path.GetFileName(glbOutputPath);
        var prefix = _options.PublicUrlPrefix.TrimEnd('/');
        return $"{prefix}/{fileName}";
    }
}
