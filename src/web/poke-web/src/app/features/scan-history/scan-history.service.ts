import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ScanBatchResult, ScanBatchHistoryEntryDto, ScanBatchItemDto, RerunScanBatchItemRequest,
  StartScanRunRequest, AddScanRunToStockResult, ScanVisionModelSettingsDto,
  ScanConfidenceStatsDto,
} from './scan-history.models';

/**
 * Scan de dossier (photos recto de carte) → historique persisté (ScanRun / ScanRunItem, voir
 * RunScanBatchCommandHandler côté backend). Pipeline : startScanRun (popup destination/motif)
 * puis runBatch (analyse DINOv2 des photos déposées dans scan-batch/_in).
 */
@Injectable({ providedIn: 'root' })
export class ScanHistoryService {
  private readonly http = inject(HttpClient);

  /** Upload les photos d'un dossier sélectionné (input webkitdirectory) vers scan-batch/_in. */
  uploadToInputFolder(files: File[]): Observable<number> {
    const formData = new FormData();
    for (const file of files) {
      formData.append('files', file, file.name);
    }
    return this.http.post<number>('/api/v1/admin/cards/scan-batch/upload', formData);
  }

  /** Ouvre un ScanRun (popup de destination) — à appeler AVANT runBatch. */
  startScanRun(request: StartScanRunRequest): Observable<string> {
    return this.http.post<string>('/api/v1/admin/cards/scan-batch/start', request);
  }

  runBatch(scanRunId: string): Observable<ScanBatchResult> {
    return this.http.post<ScanBatchResult>(`/api/v1/admin/cards/scan-batch/${scanRunId}/run`, {});
  }

  getHistory(): Observable<ScanBatchHistoryEntryDto[]> {
    return this.http.get<ScanBatchHistoryEntryDto[]>('/api/v1/admin/cards/scan-batch/history');
  }

  getRunDetail(runId: string): Observable<ScanBatchItemDto[]> {
    return this.http.get<ScanBatchItemDto[]>(`/api/v1/admin/cards/scan-batch/history/${runId}`);
  }

  archivedPhotoUrl(runId: string, fileName: string): string {
    return `/api/v1/admin/cards/scan-batch/history/${runId}/photos/${encodeURIComponent(fileName)}`;
  }

  /** Relance DINOv2 sur les photos cochées comme refusées, en excluant la carte proposée pour chacune. */
  rerunItems(runId: string, items: RerunScanBatchItemRequest[]): Observable<ScanBatchItemDto[]> {
    return this.http.post<ScanBatchItemDto[]>(`/api/v1/admin/cards/scan-batch/history/${runId}/rerun`, items);
  }

  /** Étape explicite "Ajouter au stock" (bouton du récap, à côté du "✕") — pour un run Boutique
   * pas encore commité. Idempotent côté backend : sans effet si déjà fait. À appeler une fois que
   * les corrections (refuser/relancer) sont terminées, pour ne jamais dédoubler le stock. */
  addToStock(runId: string): Observable<AddScanRunToStockResult> {
    return this.http.post<AddScanRunToStockResult>(`/api/v1/admin/cards/scan-batch/history/${runId}/add-to-stock`, {});
  }

  /** Modèle Ollama Vision utilisé par le fallback texte/vision (3e tentative et au-delà) et le
   * scan mobile — Paramètres › Scan (IA Vision), comparatif llava:7b / llava-phi3 / moondream. */
  getVisionModelSettings(): Observable<ScanVisionModelSettingsDto> {
    return this.http.get<ScanVisionModelSettingsDto>('/api/v1/admin/cards/scan-batch/vision-model');
  }

  updateVisionModelSettings(model: string): Observable<ScanVisionModelSettingsDto> {
    return this.http.put<ScanVisionModelSettingsDto>('/api/v1/admin/cards/scan-batch/vision-model', { model });
  }

  /** Indice de confiance agrégé sur tout l'historique persisté (et non plus seulement le dernier
   * run) — voir ScanConfidenceStatsDto. */
  getConfidenceStats(): Observable<ScanConfidenceStatsDto> {
    return this.http.get<ScanConfidenceStatsDto>('/api/v1/admin/cards/scan-batch/confidence-stats');
  }
}
