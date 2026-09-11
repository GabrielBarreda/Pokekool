using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.RerunScanBatchItems;

/// <summary>Une photo d'un run passé à ré-analyser, en excluant la carte proposée refusée.
/// FileName = nom d'origine (clé de fusion vers le ScanRunItem correspondant) ;
/// ArchivedPhotoFileName = nom réel du fichier sur disque dans _history/&lt;archiveId&gt;/ (peut
/// différer en cas de collision de nom).</summary>
public sealed record RerunScanBatchItemRequest(string FileName, string ArchivedPhotoFileName, Guid ExcludedCardId);

/// <summary>
/// Ré-analyse ciblée : depuis la modal de détail d'un run, l'utilisateur coche les cartes mal
/// reconnues ("refus") — ce handler relance DINOv2 uniquement sur ces photos archivées
/// (scan-batch/_history/), en excluant la carte précédemment proposée pour chacune, et met à
/// jour les ScanRunItem correspondants en base. Même mécanisme d'exclusion que
/// ScanCollectionCommand côté mobile.
/// </summary>
public sealed record RerunScanBatchItemsCommand(
    Guid ScanRunId,
    IReadOnlyList<RerunScanBatchItemRequest> Items) : IRequest<IReadOnlyList<ScanBatchItemDto>>;
