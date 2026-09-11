namespace Pokekool.Shared.Dtos.Collection;

/// <summary>
/// Résultat d'une carte détectée lors d'un scan photo/vidéo, après matching en base.
/// </summary>
public sealed record CardScanMatchDto(
    Guid? CardId,
    string? CardTcgId,
    string? CardName,
    string? DisplayName,
    string? SetName,
    string? CardNumber,
    string? ImageSmall,
    string? GradingCompany,
    decimal? GradeValue,
    string? CertNumber,
    double Confidence,
    bool Matched
);

/// <summary>
/// Statut d'un scan, pour distinguer les causes d'un résultat vide côté client
/// (au lieu d'un unique message générique "aucune carte").
/// </summary>
public enum ScanStatus
{
    /// <summary>Au moins une carte reconnue au-dessus du seuil de confiance.</summary>
    Ok,
    /// <summary>Le modèle n'a détecté aucune carte sur l'image.</summary>
    NoCardDetected,
    /// <summary>Des cartes ont été détectées mais toutes sous le seuil de confiance.</summary>
    LowConfidence,
    /// <summary>Service d'analyse (Ollama Vision) injoignable ou en timeout.</summary>
    ServiceUnavailable,
    /// <summary>Modèle vision absent côté Ollama (nécessite un `ollama pull`).</summary>
    ModelNotFound,
    /// <summary>Le modèle a répondu mais dans un format illisible.</summary>
    InvalidResponse
}

/// <summary>
/// Réponse de l'endpoint de scan : cartes détectées + statut diagnostique.
/// <para><see cref="Status"/>, <see cref="Message"/> et <see cref="RawDetections"/> sont
/// des ajouts rétrocompatibles (valeurs par défaut) : les anciens clients qui ne lisent
/// que <see cref="Matches"/> continuent de fonctionner.</para>
/// </summary>
public sealed record ScanCollectionResult(
    IReadOnlyList<CardScanMatchDto> Matches,
    ScanStatus Status = ScanStatus.Ok,
    string? Message = null,
    int RawDetections = 0);

/// <summary>Résultat de l'ajout groupé à la collection.</summary>
public sealed record BulkAddResult(int Added, int AlreadyOwned);
