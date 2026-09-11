namespace Pokekool.Shared.Dtos.Collection;

/// <summary>Une ligne d'historique (résumé) — un run d'analyse de dossier persisté (ScanRun).
/// Le détail (cartes + photos archivées) est lu séparément via Id, voir
/// IScanRunRepository.GetWithItemsAsync.</summary>
public sealed record ScanBatchHistoryEntryDto(
    Guid Id,
    DateTimeOffset RunAt,
    string DestinationType,
    bool? AddedToStockDirectly,
    string? ThirdPartyName,
    string? Reason,
    string? ReasonOtherText,
    int TotalPhotos,
    int Succeeded,
    int Failed,
    double? AverageDistance,
    long AverageElapsedMs,
    /// <summary>Pertinent uniquement si DestinationType = Boutique — classeur (StorageBinder,
    /// non rangé) auto-créé pour ce run.</summary>
    Guid? CreatedBinderId
);
