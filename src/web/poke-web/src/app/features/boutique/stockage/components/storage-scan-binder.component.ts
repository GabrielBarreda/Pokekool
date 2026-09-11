import {
  Component, inject, input, output, signal, computed, OnDestroy, ElementRef, viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from '../storage.service';
import { ToastService } from '@shared/services/toast.service';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { StorageBinderDto, StorageBoxDto, CardScanMatchDto } from '../storage.models';

type Phase = 'camera' | 'analyzing' | 'review' | 'saving';

/**
 * Overlay de scan caméra d'une double page de classeur (jusqu'à 18 pochettes).
 * Réutilise le pipeline vision (ICardVisionAnalyzer → POST /storage/scan-binder).
 * Flux : capture → analyse → validation humaine → création des StorageLocations.
 */
@Component({
  selector: 'app-storage-scan-binder',
  standalone: true,
  imports: [FormsModule, SpinnerComponent],
  template: `
    <div class="backdrop" (click)="onBackdropClick($event)">
      <div class="modal" role="dialog" aria-modal="true" aria-label="Scanner un classeur">
        <div class="modal__head">
          <div class="modal__title-group">
            <h2 class="modal__title">Scanner un classeur</h2>
            <span class="modal__sub">Capturez une double page — jusqu'à 18 cartes</span>
          </div>
          <button class="modal__close" (click)="close.emit()" aria-label="Fermer">✕</button>
        </div>

        <div class="modal__body">
          @switch (phase()) {

            <!-- PHASE 1 : caméra -->
            @case ('camera') {
              <div class="camera-section">
                <div class="video-wrap" [class.video-wrap--ready]="cameraReady()">
                  <video #videoEl class="video" autoplay playsinline muted></video>
                  <canvas #canvasEl class="canvas" hidden></canvas>
                  @if (!cameraReady()) {
                    <div class="cam-placeholder">
                      <svg viewBox="0 0 24 24" width="48" height="48" fill="none" stroke="currentColor" stroke-width="1.5">
                        <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/>
                        <circle cx="12" cy="13" r="4"/>
                      </svg>
                      <p>Autorisation caméra requise</p>
                    </div>
                  }
                </div>

                <div class="camera-controls">
                  @if (!cameraReady()) {
                    <button class="btn-primary" (click)="startCamera()">Activer la caméra</button>
                  } @else {
                    <p class="cam-hint">Cadrez la double page du classeur puis capturez.</p>
                    <button class="btn-capture" (click)="capture()">
                      <svg viewBox="0 0 16 16" width="18" height="18"><circle cx="8" cy="8" r="5.5" stroke="currentColor" stroke-width="1.5" fill="none"/><circle cx="8" cy="8" r="3" fill="currentColor"/></svg>
                      Capturer et analyser
                    </button>
                  }
                  @if (cameraError()) {
                    <p class="error">{{ cameraError() }}</p>
                  }
                </div>

                <!-- Sélection du classeur cible -->
                <div class="binder-pick">
                  <label class="pick-label">Classeur cible</label>
                  <select class="binder-select" [(ngModel)]="targetBinderId">
                    <option value="">— Créer un nouveau classeur —</option>
                    @for (b of binders(); track b.id) {
                      <option [value]="b.id">{{ b.label }}</option>
                    }
                  </select>
                  @if (!targetBinderId) {
                    <input class="binder-name-input" placeholder="Nom du nouveau classeur…"
                           [(ngModel)]="newBinderLabel" />
                  }
                </div>
              </div>
            }

            <!-- PHASE 2 : analyse en cours -->
            @case ('analyzing') {
              <div class="center">
                <app-spinner />
                <p class="analyzing-text">Analyse en cours…<br><small>Le modèle vision identifie les cartes.</small></p>
              </div>
            }

            <!-- PHASE 3 : révision humaine -->
            @case ('review') {
              <div class="review-section">
                <p class="review-intro">
                  {{ matchedCount() }} carte(s) identifiée(s) sur {{ matches().length }} détectées.
                  Décochez celles à exclure.
                </p>
                <ul class="match-list">
                  @for (m of matches(); track $index; let i = $index) {
                    <li class="match-item" [class.match-item--unmatched]="!m.matched"
                        [class.match-item--excluded]="!reviewSelected()[i]">
                      <label class="match-check">
                        <input type="checkbox" [checked]="reviewSelected()[i]"
                               [disabled]="!m.matched"
                               (change)="toggleReview(i)" />
                      </label>
                      <div class="match-img-wrap">
                        @if (m.imageSmall) {
                          <img [src]="m.imageSmall" [alt]="m.displayName || m.cardName || ''" class="match-img" loading="lazy" />
                        } @else {
                          <div class="match-no-img">?</div>
                        }
                      </div>
                      <div class="match-info">
                        <span class="match-name">{{ m.displayName || m.cardName || 'Non identifiée' }}</span>
                        @if (m.setName) { <span class="match-set">{{ m.setName }}</span> }
                        @if (m.gradingCompany) { <span class="match-grade">{{ m.gradingCompany }} {{ m.gradeValue }}</span> }
                        <div class="match-conf" [class.match-conf--low]="m.confidence < 0.65">
                          {{ m.matched ? '✓ Identifiée' : '? Non identifiée' }}
                          · {{ (m.confidence * 100).toFixed(0) }}%
                        </div>
                      </div>
                    </li>
                  }
                </ul>
              </div>
            }

            <!-- PHASE 4 : sauvegarde -->
            @case ('saving') {
              <div class="center">
                <app-spinner />
                <p class="analyzing-text">Création des assignations…</p>
              </div>
            }

          }
        </div>

        <div class="modal__foot">
          @switch (phase()) {
            @case ('camera') {
              <button class="btn-secondary" (click)="close.emit()">Annuler</button>
            }
            @case ('review') {
              <button class="btn-secondary" (click)="phase.set('camera'); stopCamera()">
                ← Reprendre
              </button>
              <button class="btn-primary"
                      [disabled]="selectedMatchCount() === 0"
                      (click)="confirmAndSave()">
                Valider {{ selectedMatchCount() }} carte(s)
              </button>
            }
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.65); z-index: 1010;
                display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .modal { background: var(--color-surface-base); border-radius: var(--radius-lg);
             width: min(720px, 96vw); max-height: 92vh; display: flex; flex-direction: column;
             box-shadow: 0 24px 64px rgba(0,0,0,.4); overflow: hidden; }

    .modal__head { display: flex; align-items: flex-start; justify-content: space-between;
                   padding: 1rem 1.25rem; border-bottom: 1px solid var(--color-border-default); gap: 1rem; }
    .modal__title-group { display: flex; flex-direction: column; gap: .15rem; }
    .modal__title { margin: 0; font-size: 1.05rem; font-weight: 700; }
    .modal__sub { font-size: .78rem; color: var(--color-text-secondary); }
    .modal__close { border: none; background: none; color: var(--color-text-secondary);
                    cursor: pointer; font-size: 1.1rem; padding: 0 .3rem; line-height: 1; flex: 0 0 auto; }
    .modal__close:hover { color: var(--color-text-primary); }

    .modal__body { flex: 1; overflow-y: auto; padding: 1rem 1.25rem; }
    .modal__foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.25rem;
                   border-top: 1px solid var(--color-border-default); background: var(--color-surface-raised); }

    /* Camera */
    .camera-section { display: flex; flex-direction: column; gap: 1rem; }
    .video-wrap { position: relative; background: #111; border-radius: var(--radius-md);
                  overflow: hidden; aspect-ratio: 16/9; }
    .video { width: 100%; height: 100%; object-fit: cover; display: block; }
    .canvas { display: none; }
    .cam-placeholder { position: absolute; inset: 0; display: flex; flex-direction: column;
                       align-items: center; justify-content: center; gap: .5rem; color: #888; }
    .cam-placeholder p { margin: 0; font-size: .85rem; }
    .camera-controls { display: flex; flex-direction: column; align-items: center; gap: .5rem; }
    .cam-hint { margin: 0; font-size: .82rem; color: var(--color-text-secondary); }
    .btn-capture { display: inline-flex; align-items: center; gap: .5rem; height: 42px; padding: 0 1.25rem;
                   border-radius: var(--radius-md); background: var(--color-brand-primary);
                   color: var(--color-brand-on-primary); border: none; cursor: pointer;
                   font-size: var(--font-size-sm); font-weight: 600; }
    .btn-capture:hover { background: var(--color-brand-primary-hover); }
    .error { color: #dc2626; font-size: .82rem; margin: 0; }

    .binder-pick { display: flex; flex-direction: column; gap: .4rem; padding: .75rem;
                   background: var(--color-surface-raised); border-radius: var(--radius-md); }
    .pick-label { font-size: .78rem; font-weight: 600; color: var(--color-text-secondary); }
    .binder-select, .binder-name-input { height: 36px; padding: 0 .5rem;
                    border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
                    background: var(--color-surface-base); color: var(--color-text-primary);
                    font-size: var(--font-size-sm); width: 100%; }
    .binder-name-input:focus { outline: none; border-color: var(--color-border-focus); }

    /* Analyzing */
    .center { display: flex; flex-direction: column; align-items: center; justify-content: center;
              padding: 3rem; gap: 1rem; }
    .analyzing-text { margin: 0; text-align: center; font-size: .88rem; color: var(--color-text-secondary);
                      line-height: 1.5; }

    /* Review */
    .review-section { display: flex; flex-direction: column; gap: .75rem; }
    .review-intro { margin: 0; font-size: .85rem; color: var(--color-text-secondary); }
    .match-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: .5rem; }
    .match-item { display: flex; align-items: center; gap: .75rem; padding: .55rem .65rem;
                  border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
                  background: var(--color-surface-base); transition: opacity .12s; }
    .match-item--unmatched { opacity: .7; }
    .match-item--excluded { opacity: .45; }
    .match-check { flex: 0 0 auto; cursor: pointer; }
    .match-img-wrap { width: 44px; height: 62px; flex: 0 0 44px; background: #f3f4f6;
                      border-radius: 4px; display: flex; align-items: center; justify-content: center;
                      overflow: hidden; }
    .match-img { width: 100%; height: 100%; object-fit: contain; }
    .match-no-img { font-size: 1.4rem; color: #ccc; }
    .match-info { flex: 1; display: flex; flex-direction: column; gap: .15rem; min-width: 0; }
    .match-name { font-weight: 600; font-size: .84rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .match-set  { font-size: .72rem; color: var(--color-text-secondary); }
    .match-grade { font-size: .72rem; color: var(--color-text-secondary); }
    .match-conf { font-size: .68rem; color: #16a34a; }
    .match-conf--low { color: #d97706; }

    .btn-primary { display: inline-flex; align-items: center; justify-content: center; gap: 6px;
                   height: 36px; padding: 0 1rem; border-radius: var(--radius-md); font-size: var(--font-size-sm);
                   cursor: pointer; border: 1px solid transparent; white-space: nowrap;
                   background: var(--color-brand-primary); color: var(--color-brand-on-primary); }
    .btn-primary:hover:not(:disabled) { background: var(--color-brand-primary-hover); }
    .btn-primary:disabled { opacity: .55; cursor: not-allowed; }
    .btn-secondary { display: inline-flex; align-items: center; justify-content: center; gap: 6px;
                     height: 36px; padding: 0 .75rem; border-radius: var(--radius-md); font-size: var(--font-size-sm);
                     cursor: pointer; border: 1px solid var(--color-border-default);
                     background: var(--color-surface-base); color: var(--color-text-primary); }
    .btn-secondary:hover { background: var(--color-surface-raised); }
  `],
})
export class StorageScanBinderComponent implements OnDestroy {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly binders = input<StorageBinderDto[]>([]);
  readonly box = input<StorageBoxDto | null>(null);

  readonly close = output<void>();
  readonly created = output<void>();

  private readonly videoEl = viewChild<ElementRef<HTMLVideoElement>>('videoEl');
  private readonly canvasEl = viewChild<ElementRef<HTMLCanvasElement>>('canvasEl');

  readonly phase = signal<Phase>('camera');
  readonly cameraReady = signal(false);
  readonly cameraError = signal<string | null>(null);
  readonly matches = signal<CardScanMatchDto[]>([]);
  readonly reviewSelected = signal<boolean[]>([]);

  targetBinderId = '';
  newBinderLabel = '';
  private stream?: MediaStream;

  readonly matchedCount = computed(() => this.matches().filter(m => m.matched).length);
  readonly selectedMatchCount = computed(() => this.reviewSelected().filter(Boolean).length);

  ngOnDestroy(): void { this.stopCamera(); }

  async startCamera(): Promise<void> {
    this.cameraError.set(null);
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment', width: { ideal: 1920 }, height: { ideal: 1080 } },
      });
      const video = this.videoEl()?.nativeElement;
      if (video) { video.srcObject = this.stream; this.cameraReady.set(true); }
    } catch {
      this.cameraError.set('Impossible d\'accéder à la caméra. Vérifiez les autorisations.');
    }
  }

  stopCamera(): void {
    this.stream?.getTracks().forEach(t => t.stop());
    this.stream = undefined;
    this.cameraReady.set(false);
  }

  capture(): void {
    const video = this.videoEl()?.nativeElement;
    const canvas = this.canvasEl()?.nativeElement;
    if (!video || !canvas) return;
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d')!.drawImage(video, 0, 0);
    canvas.toBlob(blob => {
      if (!blob) return;
      this.stopCamera();
      this.phase.set('analyzing');
      const form = new FormData();
      form.append('image', blob, 'scan.jpg');
      this.service.scanBinder(form).subscribe({
        next: result => {
          const ms = result.matches ?? [];
          this.matches.set(ms);
          this.reviewSelected.set(ms.map(m => m.matched));
          this.phase.set('review');
        },
        error: () => {
          this.phase.set('camera');
          this.toast.error('Erreur lors de l\'analyse — vérifiez le service vision.');
        },
      });
    }, 'image/jpeg', 0.92);
  }

  toggleReview(i: number): void {
    this.reviewSelected.update(arr => {
      const next = [...arr];
      next[i] = !next[i];
      return next;
    });
  }

  confirmAndSave(): void {
    const selected = this.reviewSelected();
    const validatedMatches = this.matches().filter((_, i) => selected[i] && this.matches()[i].matched);
    if (validatedMatches.length === 0) return;

    // Récupérer ou créer le classeur cible, puis assigner
    const binderId = this.targetBinderId;
    if (binderId) {
      this.doAssign(binderId, validatedMatches.map(m => m.cardId!).filter(Boolean));
    } else {
      if (!this.newBinderLabel.trim()) {
        this.toast.error('Veuillez nommer le nouveau classeur.');
        return;
      }
      const box = this.box();
      this.phase.set('saving');
      this.service.createBinder({
        label: this.newBinderLabel.trim(),
        boxId: box?.id ?? null,
        positionInBox: null,
        furnitureId: null,
        levelIndex: null,
      }).subscribe({
        next: binder => this.doAssign(binder.id, validatedMatches.map(m => m.cardId!).filter(Boolean)),
        error: () => { this.phase.set('review'); this.toast.error('Impossible de créer le classeur.'); },
      });
    }
  }

  private doAssign(binderId: string, cardIds: string[]): void {
    this.phase.set('saving');
    // Utilise le batch assign (les cardIds sont des shopStockIds ici — le backend gère via create-from-scan)
    this.service.createFromScan(binderId, 'shop_stock', cardIds).subscribe({
      next: () => this.created.emit(),
      error: () => { this.phase.set('review'); this.toast.error('Erreur lors de la création des assignations.'); },
    });
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('backdrop')) this.close.emit();
  }
}
