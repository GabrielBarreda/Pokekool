import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { CardComponent } from '@shared/ui/card/card.component';
import { ToastService } from '@shared/services/toast.service';
import { ScanHistoryService } from './scan-history.service';
import {
  ScanBatchResult, ScanBatchHistoryEntryDto, ScanBatchItemDto, RerunScanBatchItemRequest,
  ScanDestinationType, ScanReason, StartScanRunRequest, ScanConfidenceStatsDto,
} from './scan-history.models';

/**
 * Page admin "Scan & Historique" — banc de test volume pour évaluer le matching visuel pur
 * (DINOv2, texte débranché — voir ScanCollectionCommandHandler) sur 10/100/1000 photos, et
 * historique des runs passés avec détail carte par carte. Voir RunScanBatchCommandHandler
 * côté backend.
 */
@Component({
  selector: 'app-scan-history',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent, CardComponent, DecimalPipe, DatePipe, FormsModule],
  template: `
    <div class="page">
      <app-page-header
        title="Scan & Historique"
        subtitle="Banc de test volume du matching visuel (DINOv2) et historique des runs"
      />

      <div class="layout">
      <div class="layout__main">

      <app-card title="Dossier d'analyse">
        <p class="hint">
          Sélectionne un dossier contenant tes photos de recto de carte — son contenu est copié
          dans <code>scan-batch/_in</code>, puis lance l'analyse. Chaque photo est encodée en
          DINOv2 et comparée au catalogue indexé — objectif : mesurer l'indice de confiance moyen
          et la vitesse d'exécution sur du volume.
        </p>

        <input
          #folderInput
          type="file"
          webkitdirectory
          multiple
          hidden
          (change)="onFolderSelected($event)"
        />

        <div class="actions">
          <button type="button" class="btn btn--secondary" [disabled]="uploading()" (click)="folderInput.click()">
            @if (uploading()) {
              ⏳ Copie en cours…
            } @else {
              📁 Sélectionner un dossier…
            }
          </button>

          <button type="button" class="btn btn--primary" [disabled]="running()" (click)="openDestinationPopup()">
            @if (running()) {
              ⏳ Analyse en cours…
            } @else {
              ▶️ Lancer l'analyse du dossier _input
            }
          </button>

          <button type="button" class="btn btn--secondary" disabled title="Bientôt disponible">
            🎥 Choisir un périphérique caméra (bientôt disponible)
          </button>
        </div>

        <p class="hint hint--muted">
          Le 3ᵉ bouton permettra de sélectionner une webcam et de prendre une photo automatique
          toutes les X secondes, déposée directement dans <code>_input</code> — fonctionnalité
          d'industrialisation prévue plus tard, pas encore câblée.
        </p>
      </app-card>

      @if (result(); as r) {
        <app-card title="Rapport du dernier run">
          <div class="stats">
            <div class="stat">
              <span class="stat__value">{{ r.totalPhotos }}</span>
              <span class="stat__label">Photos analysées</span>
            </div>
            <div class="stat">
              <span class="stat__value stat__value--ok">{{ r.succeeded }}</span>
              <span class="stat__label">Réussies</span>
            </div>
            <div class="stat">
              <span class="stat__value" [class.stat__value--danger]="r.failed > 0">{{ r.failed }}</span>
              <span class="stat__label">Échouées</span>
            </div>
            <div class="stat">
              <span class="stat__value">
                {{ r.averageDistance !== null ? (r.averageDistance | number: '1.4-4') : '—' }}
              </span>
              <span class="stat__label">Distance moy. (cosine)</span>
            </div>
            <div class="stat">
              <span class="stat__value">{{ r.averageElapsedMs | number: '1.0-0' }} ms</span>
              <span class="stat__label">Temps moy. / photo</span>
            </div>
          </div>

          <button type="button" class="btn btn--secondary" (click)="openRunDetail(r.runId)">
            🔍 Voir le détail carte par carte
          </button>
        </app-card>
      }

      <app-card title="Historique des runs">
        @if (loadingHistory()) {
          <p class="hint hint--muted">Chargement…</p>
        } @else if (history().length === 0) {
          <p class="hint hint--muted">Aucun run enregistré pour l'instant.</p>
        } @else {
          <table class="report-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Origine</th>
                <th>Photos</th>
                <th>Réussies</th>
                <th>Échouées</th>
                <th>Distance moy.</th>
                <th>Temps moy.</th>
              </tr>
            </thead>
            <tbody>
              @for (entry of history(); track entry.id) {
                <tr class="report-table__row--clickable" (click)="openRunDetail(entry.id)">
                  <td>{{ entry.runAt | date: 'dd/MM/yyyy HH:mm' }}</td>
                  <td>{{ originLabel(entry) }}</td>
                  <td>{{ entry.totalPhotos }}</td>
                  <td>{{ entry.succeeded }}</td>
                  <td [class.report-table__error]="entry.failed > 0">{{ entry.failed }}</td>
                  <td>{{ entry.averageDistance !== null ? (entry.averageDistance | number: '1.4-4') : '—' }}</td>
                  <td>{{ entry.averageElapsedMs | number: '1.0-0' }} ms</td>
                </tr>
              }
            </tbody>
          </table>
          <p class="hint hint--muted">Clique une ligne pour voir le détail carte par carte.</p>
        }
      </app-card>

      </div>

      <!-- Sidebar : confiance + périphérique -->
      <div class="layout__side">
        <app-card title="Indice de confiance">
          <div class="confidence">
            <div class="confidence__pie" [style.background]="confidencePieBackground()">
              <div class="confidence__pie-hole">
                <span class="confidence__value">
                  {{ confidencePercent() !== null ? (confidencePercent()! | number: '1.0-0') + '%' : '—' }}
                </span>
              </div>
            </div>
            <span class="confidence__label">
              @if (confidenceSampleSize(); as n) {
                Basé sur tout l'historique ({{ n }} photo(s))
              } @else {
                Lance une analyse pour voir l'indice de confiance
              }
            </span>
          </div>
        </app-card>

        <app-card title="Périphérique branché">
          <div class="device-placeholder">
            <span class="device-placeholder__icon">📷</span>
            <span class="device-placeholder__title">Aucun périphérique connecté</span>
            <span class="device-placeholder__hint">
              Sélectionne une webcam pour activer le scan en continu (bientôt disponible).
            </span>
          </div>
        </app-card>
      </div>
      </div>
    </div>

    <!-- Pipeline "Analyse du dossier" : popups de destination/motif avant de lancer l'analyse -->
    @if (popupStep(); as step) {
      <div class="modal-overlay" (click)="cancelPopup()">
        <div class="modal modal--narrow" (click)="$event.stopPropagation()">
          <div class="modal__header">
            <h2>Analyse du dossier</h2>
            <button type="button" class="modal__close" (click)="cancelPopup()">✕</button>
          </div>

          <div class="modal__body">
            @if (step === 'destination') {
              <p class="hint">La collection appartient-elle à la boutique ?</p>
              <div class="popup-actions">
                <button type="button" class="btn btn--secondary" (click)="chooseDestination('Tiers')">Non — collection tierce</button>
                <button type="button" class="btn btn--primary" (click)="chooseDestination('Boutique')">Oui — boutique</button>
              </div>
            }

            @if (step === 'thirdParty') {
              <label class="field">
                <span class="field__label">Nom de la collection tierce</span>
                <input
                  type="text"
                  class="field__input"
                  [ngModel]="thirdPartyNameInput()"
                  (ngModelChange)="thirdPartyNameInput.set($event)"
                  placeholder="Ex : Collection de M. Dupont"
                  autofocus
                />
              </label>

              <label class="field">
                <span class="field__label">Motif du scan</span>
                <select class="field__input" [ngModel]="reasonInput()" (ngModelChange)="reasonInput.set($event)">
                  <option [ngValue]="null" disabled>Choisir un motif…</option>
                  <option [ngValue]="'Rachat'">Rachat</option>
                  <option [ngValue]="'Estimation'">Estimation</option>
                  <option [ngValue]="'Autre'">Autre</option>
                </select>
              </label>

              @if (reasonInput() === 'Autre') {
                <label class="field">
                  <span class="field__label">Préciser le motif</span>
                  <input
                    type="text"
                    class="field__input"
                    [ngModel]="reasonOtherTextInput()"
                    (ngModelChange)="reasonOtherTextInput.set($event)"
                    placeholder="Motif…"
                  />
                </label>
              }

              @if (thirdPartyPopupError(); as err) {
                <p class="hint hint--error">{{ err }}</p>
              }

              <div class="popup-actions">
                <button type="button" class="btn btn--secondary" (click)="cancelPopup()">Annuler</button>
                <button type="button" class="btn btn--primary" (click)="confirmThirdParty()">Valider</button>
              </div>
            }
          </div>
        </div>
      </div>
    }

    <!-- Modal détail d'un run -->
    @if (detailRunId(); as runId) {
      <div class="modal-overlay" (click)="closeRunDetail()">
        <div class="modal" (click)="$event.stopPropagation()">
          <div class="modal__header">
            <h2>Détail du run — {{ runId }}</h2>
            <div class="modal__header-actions">
              @if (detailRunEntry(); as entry) {
                @if (entry.destinationType === 'Boutique' && !entry.createdBinderId) {
                  <button
                    type="button"
                    class="btn btn--primary btn--sm"
                    [disabled]="addingToStock()"
                    (click)="addToStock(runId)"
                  >
                    @if (addingToStock()) {
                      ⏳ Ajout en cours…
                    } @else {
                      🏪 Ajouter au stock
                    }
                  </button>
                }
              }
              <button type="button" class="modal__close" (click)="closeRunDetail()">✕</button>
            </div>
          </div>

          @if (refusedCount() > 0) {
            <div class="modal__action-bar">
              <span>{{ refusedCount() }} carte(s) marquée(s) comme refusée(s)</span>
              <button type="button" class="btn btn--primary" [disabled]="rerunning()" (click)="rerunRefused(runId)">
                @if (rerunning()) {
                  ⏳ Ré-analyse en cours…
                } @else {
                  🔁 Relancer les refusés
                }
              </button>
            </div>
          }

          @if (detailRunEntry(); as entry) {
            @if (entry.destinationType === 'Boutique' && entry.createdBinderId) {
              <div class="modal__info-bar">
                🏪 Cartes ajoutées au stock — classeur <code>{{ entry.createdBinderId }}</code> créé
                (non rangé, à placer depuis l'écran Stockage 3D).
              </div>
            }
            @if (entry.destinationType === 'Tiers' && detailTotalEstimatedPrice() !== null) {
              <div class="modal__info-bar">
                👤 {{ entry.thirdPartyName }} — valeur estimée du lot :
                <strong>{{ detailTotalEstimatedPrice() | number: '1.2-2' }} €</strong>
                <span class="hint hint--muted"> (prix RAW catalogue, hors gradation)</span>
              </div>
            }
          }

          <div class="modal__body">
            @if (loadingDetail()) {
              <p class="hint hint--muted">Chargement…</p>
            } @else if (detailItems().length === 0) {
              <p class="hint hint--muted">Aucune carte dans ce run.</p>
            } @else {
              @for (item of detailItems(); track item.fileName) {
                <div class="detail-row">
                  @if (item.success) {
                    <label class="detail-row__checkbox" title="Marquer comme refusée">
                      <input
                        type="checkbox"
                        [checked]="isRefused(item.fileName)"
                        (change)="toggleRefused(item.fileName)"
                      />
                    </label>
                  } @else {
                    <span class="detail-row__checkbox detail-row__checkbox--spacer"></span>
                  }

                  <div class="detail-row__images">
                    <div class="detail-row__image-block">
                      <span class="detail-row__image-label">Photo scannée</span>
                      @if (item.archivedPhotoFileName) {
                        <img
                          class="detail-row__image"
                          [src]="photoUrl(runId, item.archivedPhotoFileName)"
                          [alt]="item.fileName"
                        />
                      } @else {
                        <div class="detail-row__image detail-row__image--missing">—</div>
                      }
                    </div>
                    <div class="detail-row__image-block">
                      <span class="detail-row__image-label">Carte en base</span>
                      @if (item.bestImageSmall) {
                        <img class="detail-row__image" [src]="item.bestImageSmall" [alt]="item.bestCardName ?? ''" />
                      } @else {
                        <div class="detail-row__image detail-row__image--missing">—</div>
                      }
                    </div>
                  </div>

                  <div class="detail-row__info">
                    @if (item.success) {
                      <span class="detail-row__name">{{ item.bestCardName }}</span>
                      <span class="detail-row__meta">
                        @if (item.bestSetName) { {{ item.bestSetName }} }
                        @if (item.bestSetReleaseYear) { · {{ item.bestSetReleaseYear }} }
                        @if (item.bestCardNumber) { · #{{ item.bestCardNumber }} }
                      </span>
                      <span class="detail-row__distance">
                        Distance cosine : {{ item.bestDistance !== null ? (item.bestDistance | number: '1.4-4') : '—' }}
                      </span>
                      @if (item.estimatedPrice !== null) {
                        <span class="detail-row__price">
                          ~{{ item.estimatedPrice | number: '1.2-2' }} {{ item.estimatedPriceCurrency ?? '€' }}
                        </span>
                      }
                    } @else {
                      <span class="detail-row__error">{{ item.error }}</span>
                    }
                    <span class="detail-row__filename">{{ item.fileName }} · {{ item.elapsedMs }} ms</span>
                  </div>
                </div>
              }
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .page { padding: var(--space-6); max-width: 1400px; display: flex; flex-direction: column; gap: var(--space-5); }

    .layout { display: flex; gap: var(--space-5); align-items: flex-start; }
    .layout__main { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: var(--space-5); }
    .layout__side {
      width: 300px; flex-shrink: 0;
      display: flex; flex-direction: column; gap: var(--space-5);
      position: sticky; top: var(--space-6);
    }
    @media (max-width: 900px) {
      .layout { flex-direction: column; }
      .layout__side { width: 100%; position: static; }
    }

    .confidence { display: flex; flex-direction: column; align-items: center; gap: var(--space-3); text-align: center; }
    .confidence__pie {
      width: 140px; height: 140px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      transition: background var(--duration-fast) var(--easing-standard);
    }
    .confidence__pie-hole {
      width: 96px; height: 96px; border-radius: 50%;
      background: var(--color-surface-raised);
      display: flex; align-items: center; justify-content: center;
    }
    .confidence__value { font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-text-primary); }
    .confidence__label { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .device-placeholder {
      display: flex; flex-direction: column; align-items: center; gap: var(--space-2);
      text-align: center; padding: var(--space-4) 0;
    }
    .device-placeholder__icon { font-size: var(--font-size-xxl, 32px); opacity: 0.4; }
    .device-placeholder__title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-secondary); }
    .device-placeholder__hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .hint { font-size: var(--font-size-sm); color: var(--color-text-secondary); margin-bottom: var(--space-4); line-height: 1.5; }
    .hint--muted { color: var(--color-text-muted); margin-top: var(--space-3); margin-bottom: 0; }
    code {
      background: var(--color-surface-raised);
      border-radius: var(--radius-sm);
      padding: 1px 5px;
      font-size: 0.92em;
    }

    .actions { display: flex; gap: var(--space-3); flex-wrap: wrap; }

    .btn {
      padding: var(--space-3) var(--space-4);
      border-radius: var(--radius-lg);
      border: none;
      font-family: var(--font-family-sans);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
      cursor: pointer;
      transition: opacity var(--duration-fast) var(--easing-standard);
    }
    .btn--primary { background: var(--color-brand-primary); color: white; }
    .btn--primary:disabled { opacity: .6; cursor: default; }
    .btn--secondary {
      background: var(--color-surface-raised);
      color: var(--color-text-primary);
      border: 1px solid var(--color-border-default);
    }
    .btn--secondary:disabled {
      cursor: not-allowed;
      color: var(--color-text-muted);
      border-style: dashed;
    }

    .stats {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
      gap: var(--space-4);
      margin-bottom: var(--space-4);
    }
    .stat { display: flex; flex-direction: column; gap: var(--space-1); }
    .stat__value { font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-text-primary); }
    .stat__value--ok { color: var(--color-semantic-success, #2e7d32); }
    .stat__value--danger { color: var(--color-semantic-danger); }
    .stat__label { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .report-table {
      width: 100%;
      border-collapse: collapse;
      font-size: var(--font-size-sm);
      margin-top: var(--space-3);
    }
    .report-table th {
      text-align: left;
      padding: var(--space-2) var(--space-3);
      color: var(--color-text-muted);
      font-weight: var(--font-weight-semibold);
      border-bottom: 1px solid var(--color-border-subtle);
    }
    .report-table td {
      padding: var(--space-2) var(--space-3);
      border-bottom: 1px solid var(--color-border-subtle);
      color: var(--color-text-secondary);
    }
    .report-table__error { color: var(--color-semantic-danger); }
    .report-table__row--clickable { cursor: pointer; }
    .report-table__row--clickable:hover td { background: var(--color-surface-raised); }

    /* Modal détail */
    .modal-overlay {
      position: fixed; inset: 0; z-index: 2000;
      background: rgba(0,0,0,0.55);
      display: flex; align-items: flex-start; justify-content: center;
      overflow-y: auto; padding: var(--space-5);
    }
    .modal {
      background: var(--color-surface-raised);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-xl);
      width: 100%; max-width: 720px;
      max-height: calc(100vh - 2 * var(--space-5));
      display: flex; flex-direction: column;
    }
    .modal--narrow { max-width: 420px; }

    .popup-actions { display: flex; justify-content: flex-end; gap: var(--space-3); margin-top: var(--space-4); }

    .field { display: flex; flex-direction: column; gap: var(--space-2); margin-bottom: var(--space-4); }
    .field__label { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .field__input {
      padding: var(--space-3);
      border-radius: var(--radius-md);
      border: 1px solid var(--color-border-default);
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      font-family: var(--font-family-sans);
      font-size: var(--font-size-sm);
    }

    .hint--error { color: var(--color-semantic-danger); }
    .modal__header {
      display: flex; align-items: center; justify-content: space-between;
      padding: var(--space-4) var(--space-5);
      border-bottom: 1px solid var(--color-border-subtle);
    }
    .modal__header h2 { font-size: var(--font-size-md); margin: 0; color: var(--color-text-primary); flex: 1; min-width: 0; }
    .modal__header-actions { display: flex; align-items: center; gap: var(--space-3); flex-shrink: 0; }
    .btn--sm { padding: var(--space-2) var(--space-3); font-size: var(--font-size-xs); }
    .modal__close {
      background: transparent; border: none; cursor: pointer;
      font-size: var(--font-size-md); color: var(--color-text-muted);
    }
    .modal__body { padding: var(--space-4) var(--space-5); overflow-y: auto; display: flex; flex-direction: column; gap: var(--space-4); }

    .modal__action-bar {
      display: flex; align-items: center; justify-content: space-between; gap: var(--space-3);
      padding: var(--space-3) var(--space-5);
      background: var(--color-brand-primary-subtle);
      border-bottom: 1px solid var(--color-border-subtle);
      font-size: var(--font-size-sm); color: var(--color-text-primary);
    }

    .modal__info-bar {
      padding: var(--space-3) var(--space-5);
      background: var(--color-surface-raised);
      border-bottom: 1px solid var(--color-border-subtle);
      font-size: var(--font-size-sm); color: var(--color-text-primary);
    }
    .modal__info-bar code {
      font-size: 0.85em;
    }

    .detail-row {
      display: flex; gap: var(--space-4); align-items: center;
      padding-bottom: var(--space-4);
      border-bottom: 1px solid var(--color-border-subtle);
    }

    .detail-row__checkbox {
      flex-shrink: 0; width: 20px; display: flex; align-items: center; justify-content: center;
      cursor: pointer;
    }
    .detail-row__checkbox--spacer { cursor: default; }
    .detail-row__checkbox input { width: 18px; height: 18px; cursor: pointer; accent-color: var(--color-brand-primary); }
    .detail-row:last-child { border-bottom: none; padding-bottom: 0; }

    .detail-row__images { display: flex; gap: var(--space-2); flex-shrink: 0; }
    .detail-row__image-block { display: flex; flex-direction: column; align-items: center; gap: var(--space-1); }
    .detail-row__image-label { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .detail-row__image {
      width: 72px; height: 100px; object-fit: cover;
      border-radius: var(--radius-md); border: 1px solid var(--color-border-default);
      background: var(--color-surface-base);
    }
    .detail-row__image--missing {
      display: flex; align-items: center; justify-content: center;
      color: var(--color-text-muted); font-size: var(--font-size-lg);
    }

    .detail-row__info { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
    .detail-row__name { font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .detail-row__meta { font-size: var(--font-size-sm); color: var(--color-text-secondary); }
    .detail-row__distance { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .detail-row__price { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-semantic-success, #2e7d32); }
    .detail-row__error { font-size: var(--font-size-sm); color: var(--color-semantic-danger); }
    .detail-row__filename { font-size: var(--font-size-xs); color: var(--color-text-muted); margin-top: var(--space-1); }
  `],
})
export class ScanHistoryComponent {
  private readonly service = inject(ScanHistoryService);
  private readonly toast = inject(ToastService);

  readonly uploading = signal(false);
  readonly running = signal(false);
  readonly result = signal<ScanBatchResult | null>(null);
  readonly history = signal<ScanBatchHistoryEntryDto[]>([]);
  readonly loadingHistory = signal(true);

  readonly detailRunId = signal<string | null>(null);
  readonly detailItems = signal<ScanBatchItemDto[]>([]);
  readonly loadingDetail = signal(false);
  readonly refusedFileNames = signal<Set<string>>(new Set());
  readonly rerunning = signal(false);
  readonly addingToStock = signal(false);

  readonly refusedCount = computed(() => this.refusedFileNames().size);

  /// Entrée d'historique correspondant au run affiché en détail — sert à savoir si c'est un run
  /// Boutique (lien vers le classeur créé) ou Tiers (affichage des prix estimés). Le run tout
  /// juste exécuté (result()) n'est pas encore dans history() au moment de l'ouverture
  /// automatique de la modal (loadHistory() est asynchrone) — on retombe alors sur l'info connue
  /// depuis le popup de destination (pendingDestination) le temps que l'historique se rafraîchisse.
  readonly detailRunEntry = computed<ScanBatchHistoryEntryDto | null>(() => {
    const runId = this.detailRunId();
    if (!runId) return null;
    return this.history().find(e => e.id === runId) ?? null;
  });

  readonly detailTotalEstimatedPrice = computed<number | null>(() => {
    const items = this.detailItems().filter(i => i.estimatedPrice !== null);
    if (items.length === 0) return null;
    return items.reduce((sum, i) => sum + (i.estimatedPrice ?? 0), 0);
  });

  // ── Pipeline "Analyse du dossier" : popup(s) de destination avant de lancer l'analyse ──
  // 'destination' → (Boutique: lance directement) | (Tiers: 'thirdParty') → null (fermé, analyse
  // lancée ou annulée). Voir StartScanRunCommand côté backend pour les règles de validation.
  readonly popupStep = signal<'destination' | 'thirdParty' | null>(null);
  readonly thirdPartyNameInput = signal('');
  readonly reasonInput = signal<ScanReason | null>(null);
  readonly reasonOtherTextInput = signal('');
  readonly thirdPartyPopupError = signal<string | null>(null);
  private pendingDestination: ScanDestinationType | null = null;

  /// Indice de confiance agrégé sur TOUT l'historique persisté (et non plus seulement le dernier
  /// run) — voir ScanConfidenceStatsDto côté backend. Corrigé le 2026-09-11 (demande explicite de
  /// Gab) : auparavant calculé uniquement à partir du run en cours de session ou du run le plus
  /// récent de l'historique, ce qui figeait l'indice dès qu'on arrêtait de scanner.
  readonly confidenceStats = signal<ScanConfidenceStatsDto | null>(null);

  readonly confidencePercent = computed<number | null>(() => this.confidenceStats()?.confidencePercent ?? null);

  readonly confidenceSampleSize = computed<number | null>(() => {
    const stats = this.confidenceStats();
    return stats && stats.totalPhotos > 0 ? stats.totalPhotos : null;
  });

  readonly confidencePieBackground = computed<string>(() => {
    const pct = this.confidencePercent();
    if (pct === null) return 'conic-gradient(var(--color-border-subtle) 0% 100%)';
    const color = pct >= 60 ? 'var(--color-semantic-success, #2e7d32)'
                : pct >= 35 ? 'var(--color-semantic-warning, #b8860b)'
                : 'var(--color-semantic-danger)';
    return `conic-gradient(${color} 0% ${pct}%, var(--color-border-subtle) ${pct}% 100%)`;
  });

  constructor() {
    this.loadHistory();
    this.loadConfidenceStats();
  }

  private loadConfidenceStats(): void {
    this.service.getConfidenceStats().subscribe({
      next: stats => this.confidenceStats.set(stats),
      error: () => {},
    });
  }

  onFolderSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const fileList = input.files;
    if (!fileList || fileList.length === 0) return;

    const files = Array.from(fileList).filter(f =>
      /\.(jpe?g|png)$/i.test(f.name)
    );

    if (files.length === 0) {
      this.toast.error("Aucune image (jpg/png) trouvée dans le dossier sélectionné.");
      input.value = '';
      return;
    }

    this.uploading.set(true);
    this.service.uploadToInputFolder(files).subscribe({
      next: saved => {
        this.uploading.set(false);
        this.toast.success(`${saved} photo(s) copiée(s) dans _input.`);
        input.value = '';
      },
      error: () => {
        this.uploading.set(false);
        this.toast.error("Échec de la copie des photos.");
        input.value = '';
      },
    });
  }

  // ── Pipeline de popups avant analyse ──────────────────────────────────────────────────
  openDestinationPopup(): void {
    if (this.running()) return;
    this.pendingDestination = null;
    this.thirdPartyNameInput.set('');
    this.reasonInput.set(null);
    this.reasonOtherTextInput.set('');
    this.thirdPartyPopupError.set(null);
    this.popupStep.set('destination');
  }

  chooseDestination(destination: ScanDestinationType): void {
    this.pendingDestination = destination;
    if (destination === 'Boutique') {
      // Plus de popup "Ajouter directement au stock ?" — le commit se fait désormais toujours
      // explicitement via le bouton "🏪 Ajouter au stock" sur le récap, une fois les éventuelles
      // corrections (refuser/relancer) terminées. addedToStockDirectly reste transmis à false
      // (le backend garde ce champ pour compat/historique, mais ne l'utilise plus pour
      // auto-commit que s'il vaut explicitement true — voir RunScanBatchCommandHandler).
      this.popupStep.set(null);
      this.startAndRun({
        destinationType: 'Boutique',
        addedToStockDirectly: false,
        thirdPartyName: null,
        reason: null,
        reasonOtherText: null,
      });
    } else {
      this.popupStep.set('thirdParty');
    }
  }

  confirmThirdParty(): void {
    const name = this.thirdPartyNameInput().trim();
    const reason = this.reasonInput();

    if (!name) {
      this.thirdPartyPopupError.set('Le nom de la collection est requis.');
      return;
    }
    if (!reason) {
      this.thirdPartyPopupError.set('Le motif est requis.');
      return;
    }
    if (reason === 'Autre' && !this.reasonOtherTextInput().trim()) {
      this.thirdPartyPopupError.set('Précise le motif.');
      return;
    }

    this.popupStep.set(null);
    this.startAndRun({
      destinationType: 'Tiers',
      addedToStockDirectly: null,
      thirdPartyName: name,
      reason,
      reasonOtherText: reason === 'Autre' ? this.reasonOtherTextInput().trim() : null,
    });
  }

  cancelPopup(): void {
    this.popupStep.set(null);
  }

  originLabel(entry: ScanBatchHistoryEntryDto): string {
    if (entry.destinationType === 'Boutique') {
      return entry.addedToStockDirectly ? '🏪 Boutique (stock direct)' : '🏪 Boutique';
    }
    const reason = entry.reason === 'Autre' ? (entry.reasonOtherText ?? 'Autre') : entry.reason;
    return `👤 ${entry.thirdPartyName ?? 'Tiers'} — ${reason ?? '—'}`;
  }

  private startAndRun(request: StartScanRunRequest): void {
    this.running.set(true);
    this.service.startScanRun(request).subscribe({
      next: scanRunId => this.runBatch(scanRunId),
      error: () => {
        this.running.set(false);
        this.toast.error("Échec de l'ouverture du run — impossible de démarrer l'analyse.");
      },
    });
  }

  private runBatch(scanRunId: string): void {
    this.service.runBatch(scanRunId).subscribe({
      next: r => {
        this.running.set(false);
        this.result.set(r);
        if (r.totalPhotos === 0) {
          this.toast.error('Aucune photo trouvée dans scan-batch/_in.');
        } else {
          this.toast.success(`${r.succeeded}/${r.totalPhotos} photo(s) analysée(s).`);
          this.loadConfidenceStats();
          // Recharge l'historique AVANT d'ouvrir la modal — detailRunEntry() (bandeau
          // classeur/prix estimé) a besoin que ce run y figure déjà.
          this.service.getHistory().subscribe({
            next: h => {
              this.history.set(h);
              this.loadingHistory.set(false);
              this.openRunDetailFromResult(r.runId, r.items);
            },
            error: () => {
              this.loadingHistory.set(false);
              this.openRunDetailFromResult(r.runId, r.items);
            },
          });
        }
      },
      error: () => {
        this.running.set(false);
        this.toast.error("Échec de l'analyse — vérifie que l'API et le service d'embedding tournent.");
      },
    });
  }

  /// Ouverture automatique de la modal de détail sur le run qui vient de tourner — évite
  /// l'aller-retour manuel "lancer → chercher la ligne dans l'historique → cliquer".
  private openRunDetailFromResult(runId: string, items: ScanBatchItemDto[]): void {
    this.detailRunId.set(runId);
    this.detailItems.set(items);
    this.loadingDetail.set(false);
    this.refusedFileNames.set(new Set());
  }

  openRunDetail(runId: string): void {
    this.detailRunId.set(runId);
    this.loadingDetail.set(true);
    this.detailItems.set([]);
    this.refusedFileNames.set(new Set());
    this.service.getRunDetail(runId).subscribe({
      next: items => { this.detailItems.set(items); this.loadingDetail.set(false); },
      error: () => {
        this.loadingDetail.set(false);
        this.toast.error('Impossible de charger le détail de ce run.');
      },
    });
  }

  closeRunDetail(): void {
    this.detailRunId.set(null);
  }

  photoUrl(runId: string, fileName: string): string {
    return this.service.archivedPhotoUrl(runId, fileName);
  }

  isRefused(fileName: string): boolean {
    return this.refusedFileNames().has(fileName);
  }

  toggleRefused(fileName: string): void {
    const next = new Set(this.refusedFileNames());
    if (next.has(fileName)) next.delete(fileName);
    else next.add(fileName);
    this.refusedFileNames.set(next);
  }

  rerunRefused(runId: string): void {
    if (this.rerunning()) return;

    const refused = this.refusedFileNames();
    const requests: RerunScanBatchItemRequest[] = this.detailItems()
      .filter(item => refused.has(item.fileName) && item.bestCardId && item.archivedPhotoFileName)
      .map(item => ({
        fileName: item.fileName,
        archivedPhotoFileName: item.archivedPhotoFileName!,
        excludedCardId: item.bestCardId!,
      }));

    if (requests.length === 0) return;

    this.rerunning.set(true);
    this.service.rerunItems(runId, requests).subscribe({
      next: updated => {
        this.rerunning.set(false);
        // Fusionne les résultats mis à jour dans la liste affichée, sans recharger toute la modal.
        const updatedByFileName = new Map(updated.map(i => [i.fileName, i]));
        this.detailItems.set(
          this.detailItems().map(item => updatedByFileName.get(item.fileName) ?? item)
        );
        this.refusedFileNames.set(new Set());
        this.toast.success(`${updated.length} carte(s) ré-analysée(s).`);
        // Le refus + relance change la répartition directe/manuelle pour ces photos — voir
        // ExcludedCardIds côté backend, signal utilisé par l'indice de confiance agrégé.
        this.loadConfidenceStats();
      },
      error: () => {
        this.rerunning.set(false);
        this.toast.error('Échec de la ré-analyse.');
      },
    });
  }

  /// Ajout explicite au stock (bouton du récap, à côté du "✕") — n'apparaît que pour un run
  /// Boutique pas encore commité (voir template : !entry.createdBinderId). Recharge l'historique
  /// après succès pour que detailRunEntry() reflète le nouveau createdBinderId (fait disparaître
  /// le bouton et affiche le bandeau "classeur créé" à la place).
  addToStock(runId: string): void {
    if (this.addingToStock()) return;

    this.addingToStock.set(true);
    this.service.addToStock(runId).subscribe({
      next: result => {
        this.addingToStock.set(false);
        if (result.alreadyCommitted) {
          this.toast.error('Ce run a déjà été ajouté au stock.');
        } else {
          this.toast.success(`${result.cardsAdded} carte(s) ajoutée(s) au stock.`);
        }
        this.service.getHistory().subscribe({
          next: h => this.history.set(h),
          error: () => {},
        });
      },
      error: () => {
        this.addingToStock.set(false);
        this.toast.error("Échec de l'ajout au stock.");
      },
    });
  }

  private loadHistory(): void {
    this.loadingHistory.set(true);
    this.service.getHistory().subscribe({
      next: h => { this.history.set(h); this.loadingHistory.set(false); },
      error: () => { this.loadingHistory.set(false); },
    });
  }
}
