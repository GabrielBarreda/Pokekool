using Microsoft.AspNetCore.Hosting;
using Pokekool.Application.Collection.Scanning;

namespace Pokekool.Infrastructure.Collection.ScanBatch;

/// <summary>
/// Implémentation disque de <see cref="IScanBatchFileStore"/> — dossiers scan-batch/_in,
/// scan-batch/_out et scan-batch/_history/&lt;archiveId&gt; sous le ContentRoot de l'API (montés
/// en volume Docker, donc accessibles directement depuis Windows en dev). Le détail structuré
/// (cartes identifiées, stats) est persisté en base via IScanRunRepository — ce store ne garde
/// que les fichiers image eux-mêmes. Voir RunScanBatchCommandHandler.
/// </summary>
public sealed class ScanBatchFileStore : IScanBatchFileStore
{
    private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png" };

    private readonly string _inPath;
    private readonly string _outPath;
    private readonly string _historyRoot;

    public ScanBatchFileStore(IWebHostEnvironment env)
    {
        var root = Path.Combine(env.ContentRootPath, "scan-batch");
        _inPath      = Path.Combine(root, "_in");
        _outPath     = Path.Combine(root, "_out");
        _historyRoot = Path.Combine(root, "_history");

        Directory.CreateDirectory(_inPath);
        Directory.CreateDirectory(_outPath);
        Directory.CreateDirectory(_historyRoot);
    }

    public IReadOnlyList<string> ListPendingPhotos()
        => Directory.EnumerateFiles(_inPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public async Task<string> ReadPhotoAsBase64Async(string filePath, CancellationToken ct = default)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return Convert.ToBase64String(bytes);
    }

    public void MoveToOut(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var dest = Path.Combine(_outPath, fileName);

        // Collision (même nom déjà présent dans _out d'un run précédent) : on suffixe plutôt
        // que d'écraser une photo/preuve d'un précédent test.
        if (File.Exists(dest))
        {
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var ext  = Path.GetExtension(fileName);
            var i    = 1;
            do { dest = Path.Combine(_outPath, $"{stem}_{i++}{ext}"); }
            while (File.Exists(dest));
        }

        File.Move(filePath, dest);
    }

    public string CopyToRunArchive(string filePath, string archiveId)
    {
        var runDir = RunDir(archiveId);
        Directory.CreateDirectory(runDir);

        var fileName = Path.GetFileName(filePath);
        var dest = Path.Combine(runDir, fileName);

        if (File.Exists(dest))
        {
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var ext  = Path.GetExtension(fileName);
            var i    = 1;
            do { dest = Path.Combine(runDir, $"{stem}_{i++}{ext}"); }
            while (File.Exists(dest));
        }

        File.Copy(filePath, dest);
        return Path.GetFileName(dest);
    }

    public Task WriteReportAsync(string csvContent, CancellationToken ct = default)
        => File.WriteAllTextAsync(Path.Combine(_outPath, "_report.csv"), csvContent, ct);

    public async Task<string> SavePendingPhotoAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        // Nom de fichier nu (pas de sous-chemin) : le sélecteur de dossier envoie potentiellement
        // un chemin relatif (ex: "Photos-cartes/img1.jpg") — on aplati volontairement, _in reste
        // un dossier plat.
        var safeName = Path.GetFileName(fileName);
        var dest = Path.Combine(_inPath, safeName);

        if (File.Exists(dest))
        {
            var stem = Path.GetFileNameWithoutExtension(safeName);
            var ext  = Path.GetExtension(safeName);
            var i    = 1;
            do { dest = Path.Combine(_inPath, $"{stem}_{i++}{ext}"); }
            while (File.Exists(dest));
        }

        await using var fileStream = File.Create(dest);
        await content.CopyToAsync(fileStream, ct);
        return Path.GetFileName(dest);
    }

    public string? GetArchivedPhotoPath(string archiveId, string fileName)
    {
        // fileName vient de l'URL : on l'aplatit par sécurité (pas de traversal ../).
        var safeName = Path.GetFileName(fileName);
        var path = Path.Combine(RunDir(archiveId), safeName);
        return File.Exists(path) ? path : null;
    }

    private string RunDir(string archiveId) => Path.Combine(_historyRoot, Path.GetFileName(archiveId));
}
