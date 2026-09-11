namespace Pokekool.Shared.Dtos.Collection;

/// <summary>Requête d'ouverture d'un ScanRun — reflète le pipeline de popups "Analyse du
/// dossier" côté web (destination boutique/tierce, ajout direct au stock, nom + motif tiers).
/// DestinationType : "Boutique" | "Tiers". Reason (si Tiers) : "Rachat" | "Estimation" | "Autre".
/// Voir StartScanRunCommand pour les règles de validation.</summary>
public sealed record StartScanRunRequest(
    string DestinationType,
    bool? AddedToStockDirectly,
    string? ThirdPartyName,
    string? Reason,
    string? ReasonOtherText);
