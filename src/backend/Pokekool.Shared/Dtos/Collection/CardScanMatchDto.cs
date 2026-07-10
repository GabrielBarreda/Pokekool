namespace Pokekool.Shared.Dtos.Collection;

/// <summary>
/// Résultat d'une carte détectée lors d'un scan photo/vidéo, après matching en base.
/// </summary>
public sealed record CardScanMatchDto(
    Guid? CardId,
    string? CardName,
    string? SetName,
    string? CardNumber,
    string? ImageSmall,
    string? GradingCompany,
    decimal? GradeValue,
    string? CertNumber,
    double Confidence,
    bool Matched
);

/// <summary>Réponse de l'endpoint de scan : liste des cartes détectées.</summary>
public sealed record ScanCollectionResult(IReadOnlyList<CardScanMatchDto> Matches);

/// <summary>Résultat de l'ajout groupé à la collection.</summary>
public sealed record BulkAddResult(int Added, int AlreadyOwned);
