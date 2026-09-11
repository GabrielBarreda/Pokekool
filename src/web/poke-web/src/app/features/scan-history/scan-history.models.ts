export type ScanDestinationType = 'Boutique' | 'Tiers';
export type ScanReason = 'Rachat' | 'Estimation' | 'Autre';

export interface ScanBatchItemDto {
  fileName: string;
  archivedPhotoFileName: string | null;
  success: boolean;
  bestCardId: string | null;
  bestCardName: string | null;
  bestSetName: string | null;
  bestSetReleaseYear: number | null;
  bestCardNumber: string | null;
  bestImageSmall: string | null;
  bestDistance: number | null;
  elapsedMs: number;
  error: string | null;
  /** Prix de marché estimé (RAW uniquement) — renseigné uniquement pour un run Tiers. */
  estimatedPrice: number | null;
  estimatedPriceCurrency: string | null;
}

export interface RerunScanBatchItemRequest {
  fileName: string;
  archivedPhotoFileName: string;
  excludedCardId: string;
}

export interface ScanBatchResult {
  runId: string;
  totalPhotos: number;
  succeeded: number;
  failed: number;
  averageDistance: number | null;
  averageElapsedMs: number;
  items: ScanBatchItemDto[];
}

/** Requête d'ouverture d'un ScanRun — reflète le popup "La collection appartient-elle à la
 * boutique ?" puis, selon la réponse, "Ajouter directement au stock ?" (Boutique) ou nom + motif
 * de la collection tierce (Tiers). Voir StartScanRunCommand côté backend. */
export interface StartScanRunRequest {
  destinationType: ScanDestinationType;
  addedToStockDirectly: boolean | null;
  thirdPartyName: string | null;
  reason: ScanReason | null;
  reasonOtherText: string | null;
}

export interface ScanBatchHistoryEntryDto {
  id: string;
  runAt: string;
  destinationType: ScanDestinationType;
  addedToStockDirectly: boolean | null;
  thirdPartyName: string | null;
  reason: ScanReason | null;
  reasonOtherText: string | null;
  totalPhotos: number;
  succeeded: number;
  failed: number;
  averageDistance: number | null;
  averageElapsedMs: number;
  /** Pertinent uniquement si destinationType = 'Boutique' — classeur (non rangé) auto-créé. */
  createdBinderId: string | null;
}

/** Réponse de l'ajout explicite au stock (bouton "🏪 Ajouter au stock" du récap) — voir
 * AddScanRunToStockCommandHandler côté backend. */
export interface AddScanRunToStockResult {
  scanRunId: string;
  createdBinderId: string | null;
  cardsAdded: number;
  alreadyCommitted: boolean;
}

/** Réglage "modèle Ollama Vision" utilisé par le fallback texte/vision du scan (3e tentative et
 * au-delà) et par le scan mobile — Paramètres › Scan (IA Vision). Voir
 * ScanVisionModelSettingsDto/VisionModelSettingsKeys côté backend. */
export interface ScanVisionModelSettingsDto {
  model: string;
  allowedModels: string[];
}

/** Indice de confiance agrégé sur TOUT l'historique persisté des scans (et non plus seulement le
 * dernier run) — widget "Indice de confiance" du récap. Voir ScanConfidenceStatsDto côté backend :
 * directMatches = reconnues du premier coup, manualMatches = reconnues après refus+relance
 * (compte comme une "erreur" du pipeline auto au sens de Gab), failures = jamais reconnues. */
export interface ScanConfidenceStatsDto {
  totalPhotos: number;
  directMatches: number;
  manualMatches: number;
  failures: number;
  confidencePercent: number | null;
}
