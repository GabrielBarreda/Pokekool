namespace Pokekool.Application.Collection.Scanning;

/// <summary>
/// Accès aux dossiers de scan de photos : scan-batch/_in (photos en attente), scan-batch/_out
/// (photos traitées + rapport) et scan-batch/_history/&lt;archiveId&gt; (archive pérenne des
/// photos elles-mêmes — le détail structuré d'un run, lui, est persisté en base via
/// IScanRunRepository, voir RunScanBatchCommandHandler). Chemin résolu en Infrastructure
/// (ContentRootPath), Application ne connaît que l'abstraction.
/// </summary>
public interface IScanBatchFileStore
{
    /// <summary>Chemins absolus des photos actuellement dans _in (jpg/jpeg/png), triés par nom.</summary>
    IReadOnlyList<string> ListPendingPhotos();

    /// <summary>Lit une photo et l'encode en base64, prêt pour ICardEmbeddingService.</summary>
    Task<string> ReadPhotoAsBase64Async(string filePath, CancellationToken ct = default);

    /// <summary>Déplace une photo traitée de _in vers _out (même nom ; renomme si collision).</summary>
    void MoveToOut(string filePath);

    /// <summary>
    /// Copie une photo traitée vers l'archive pérenne du run (_history/&lt;archiveId&gt;/) —
    /// appelée AVANT MoveToOut puisque MoveToOut déplace le fichier source. archiveId est l'Id
    /// du ScanRun (Guid.ToString()) — voir RunScanBatchCommandHandler. Retourne le nom de
    /// fichier archivé (pour ScanRunItem.ArchivedPhotoFileName).
    /// </summary>
    string CopyToRunArchive(string filePath, string archiveId);

    /// <summary>Écrit/écrase le rapport CSV du dernier run dans _out/_report.csv.</summary>
    Task WriteReportAsync(string csvContent, CancellationToken ct = default);

    /// <summary>
    /// Écrit une photo uploadée (sélecteur de dossier Windows côté web) dans _in — renomme en
    /// cas de collision plutôt que d'écraser. Retourne le nom de fichier final utilisé.
    /// </summary>
    Task<string> SavePendingPhotoAsync(string fileName, Stream content, CancellationToken ct = default);

    /// <summary>Chemin absolu d'une photo archivée d'un run (archiveId = ScanRun.Id.ToString()),
    /// ou null si elle n'existe pas.</summary>
    string? GetArchivedPhotoPath(string archiveId, string fileName);
}
