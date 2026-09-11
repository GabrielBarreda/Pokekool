namespace Pokekool.Shared.Dtos.Collection;

/// <summary>
/// Résultat d'analyse d'une photo du dossier _in (banc de test volume DINOv2, 2026-09-08).
/// Une ligne par photo — voir 03-Metier/Automatisations-LLM.md § Matching visuel (DINOv2).
/// </summary>
public sealed record ScanBatchItemDto(
    string FileName,
    string? ArchivedPhotoFileName,
    bool Success,
    Guid? BestCardId,
    string? BestCardName,
    string? BestSetName,
    int? BestSetReleaseYear,
    string? BestCardNumber,
    string? BestImageSmall,
    double? BestDistance,
    long ElapsedMs,
    string? Error,
    /// <summary>Prix de marché estimé (RAW uniquement) — renseigné uniquement pour un ScanRun de
    /// destination Tiers, voir RunScanBatchCommandHandler.</summary>
    decimal? EstimatedPrice = null,
    string? EstimatedPriceCurrency = null
);

/// <summary>Résultat global d'une exécution du batch _in → _out — RunId référence le ScanRun
/// persisté (voir StartScanRunCommand + ScanRun), qui porte la destination/motif du run.</summary>
public sealed record ScanBatchResult(
    Guid RunId,
    int TotalPhotos,
    int Succeeded,
    int Failed,
    double? AverageDistance,
    long AverageElapsedMs,
    IReadOnlyList<ScanBatchItemDto> Items
);

/// <summary>Résultat de l'ajout explicite au stock d'un run Boutique (bouton "🏪 Ajouter au
/// stock" du récap) — voir AddScanRunToStockCommandHandler. AlreadyCommitted = true si le run
/// avait déjà un classeur (idempotence, aucune carte re-ajoutée).</summary>
public sealed record AddScanRunToStockResult(
    Guid ScanRunId,
    Guid? CreatedBinderId,
    int CardsAdded,
    bool AlreadyCommitted
);

/// <summary>Réglage "modèle Ollama Vision" utilisé par le fallback texte/vision du scan (voir
/// VisionModelSettingsKeys côté backend) — Paramètres › Scan (IA Vision). AllowedModels expose la
/// liste au frontend pour peupler le combobox sans la dupliquer côté Angular.</summary>
public sealed record ScanVisionModelSettingsDto(
    string Model,
    IReadOnlyList<string> AllowedModels
);

/// <summary>
/// Indice de confiance agrégé sur TOUT l'historique persisté (et non plus seulement le dernier
/// run) — voir GetScanConfidenceStatsQueryHandler. Distingue trois issues par photo scannée,
/// cumulées sur tous les ScanRunItem existants :
/// - DirectMatches : reconnue du premier coup (Success, aucun refus/relance — ExcludedCardIds vide).
/// - ManualMatches : reconnue après au moins un refus + relance (Success, ExcludedCardIds non vide)
///   — compte comme une "erreur" du pipeline automatique au sens où Gab l'entend : la carte n'a
///   été retrouvée que grâce à une correction manuelle.
/// - Failures : jamais reconnue (Success = false), quel que soit le nombre de tentatives.
/// ConfidencePercent = DirectMatches / TotalPhotos (reconnaissance directe uniquement — même
/// définition stricte que l'ancien indice "1 - distance moyenne", mais désormais sur l'historique
/// complet plutôt que le seul dernier run).
/// </summary>
public sealed record ScanConfidenceStatsDto(
    int TotalPhotos,
    int DirectMatches,
    int ManualMatches,
    int Failures,
    double? ConfidencePercent
);
