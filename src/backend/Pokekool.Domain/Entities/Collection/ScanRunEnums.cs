namespace Pokekool.Domain.Entities.Collection;

/// <summary>
/// Destination d'un run de scan (banc de test volume, "Analyse du dossier") : collection propre
/// de la boutique, ou collection tierce apportée par un potentiel vendeur (estimation, rachat...).
/// Choisi via le popup d'ouverture du run — voir StartScanRunCommand.
/// </summary>
public enum ScanDestinationType
{
    Boutique = 1,
    Tiers    = 2,
}

/// <summary>
/// Motif d'un scan de collection tierce (uniquement pertinent quand DestinationType = Tiers —
/// voir ScanRun.Reason). "Autre" s'accompagne d'un texte libre (ScanRun.ReasonOtherText).
/// </summary>
public enum ScanReason
{
    Rachat     = 1,
    Estimation = 2,
    Autre      = 3,
}
