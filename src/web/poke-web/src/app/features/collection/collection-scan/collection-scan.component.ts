import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CollectionService } from '../collection.service';
import { CardScanMatchDto } from '../collection.models';
import { ToastService } from '@shared/services/toast.service';

const OPENCV_CDN = 'https://docs.opencv.org/4.8.0/opencv.js';

@Component({
  selector: 'app-collection-scan',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DecimalPipe],
  styles: [`
    :host { display: block; }

    .scan-container { display: flex; flex-direction: column; gap: var(--space-4); }

    .viewport {
      position: relative;
      width: 100%;
      max-width: 800px;
      margin: 0 auto;
      background: #000;
      border-radius: var(--radius-lg);
      overflow: hidden;
    }

    video { width: 100%; display: block; }

    .overlay {
      position: absolute;
      inset: 0;
      pointer-events: none;
      width: 100%;
      height: 100%;
    }

    .guide-text {
      position: absolute;
      bottom: var(--space-3);
      left: 50%;
      transform: translateX(-50%);
      background: rgba(0,0,0,0.6);
      color: #fff;
      padding: var(--space-1) var(--space-3);
      border-radius: var(--radius-full);
      font-size: var(--font-size-sm);
      white-space: nowrap;
    }

    .scan-bar { display: flex; gap: var(--space-3); justify-content: center; }

    .review-panel { display: flex; flex-direction: column; gap: var(--space-4); margin-top: var(--space-5); }
    .review-panel h2 { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; }
    .results-list { display: flex; flex-direction: column; gap: var(--space-2); }

    .result-row {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-3);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-md);
      background: var(--color-surface-base);
    }
    .result-row.matched   { border-left: 3px solid var(--color-semantic-success); }
    .result-row.unmatched { border-left: 3px solid var(--color-semantic-warning); opacity: 0.7; }

    .card-thumb { width: 50px; height: 70px; object-fit: contain; border-radius: var(--radius-sm); }
    .card-thumb.placeholder { display: flex; align-items: center; justify-content: center;
      background: var(--color-surface-sunken); font-size: 24px; }

    .card-info { display: flex; flex-direction: column; gap: var(--space-1); flex: 1; }
    .card-name { font-weight: var(--font-weight-semibold); font-size: var(--font-size-sm); }
    .card-meta { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .confidence { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .badge { display: inline-flex; align-items: center; padding: 2px 8px;
      border-radius: var(--radius-full); font-size: var(--font-size-xs); font-weight: 500; width: fit-content; }
    .badge--match   { background: rgba(34,197,94,0.15); color: #16a34a; }
    .badge--nomatch { background: rgba(234,179,8,0.15);  color: #ca8a04; }
    .badge--grade   { background: rgba(99,102,241,0.15); color: #4f46e5; }

    .review-actions { display: flex; justify-content: flex-end; gap: var(--space-3); }

    .btn-primary { height: 40px; padding: 0 var(--space-5); border: none; border-radius: var(--radius-md);
      background: var(--color-brand-primary); color: #fff; cursor: pointer; font-size: var(--font-size-sm); }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-ghost { height: 40px; padding: 0 var(--space-4); border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); background: transparent; color: var(--color-text-primary);
      cursor: pointer; font-size: var(--font-size-sm); }
    .error { color: var(--color-semantic-danger); font-size: var(--font-size-sm); }
  `],
  template: `
    <div class="scan-container">
      <div class="viewport">
        <video #videoEl autoplay playsinline muted></video>
        <canvas #overlayEl class="overlay"></canvas>

        <div class="guide-text">
          @if (stableCount() === 0) {
            Posez les cartes face visible dans le cadre
          } @else {
            {{ stableCount() }} carte(s) détectée(s) — prêt à capturer
          }
        </div>
      </div>

      <div class="scan-bar">
        @if (cameraError()) {
          <p class="error">⚠ {{ cameraError() }}</p>
          <button class="btn-ghost" (click)="close()">Fermer</button>
        } @else {
          <button
            class="btn-primary"
            [disabled]="stableCount() === 0 || analyzing()"
            (click)="capture()"
          >
            {{ analyzing() ? '🔍 Analyse en cours…' : '📸 Capturer et analyser' }}
          </button>
          <button class="btn-ghost" (click)="close()">Annuler</button>
        }
      </div>
    </div>

    @if (phase() === 'review') {
      <div class="review-panel">
        <h2>Cartes détectées</h2>

        <div class="results-list">
          @for (card of results(); track $index) {
            <div class="result-row" [class.matched]="card.matched" [class.unmatched]="!card.matched">
              <input
                type="checkbox"
                [checked]="selected()[$index]"
                [disabled]="!card.matched"
                (change)="toggleSelect($index)"
              />

              @if (card.imageSmall) {
                <img [src]="card.imageSmall" alt="" class="card-thumb" />
              } @else {
                <div class="card-thumb placeholder">🃏</div>
              }

              <div class="card-info">
                <span class="card-name">{{ card.displayName ?? card.cardName ?? card.setName ?? 'Inconnue' }}</span>
                <span class="card-meta">{{ card.setName }} · {{ card.cardNumber }}</span>

                @if (card.gradingCompany) {
                  <span class="badge badge--grade">
                    {{ card.gradingCompany }} {{ card.gradeValue }} · #{{ card.certNumber }}
                  </span>
                }

                <span class="badge" [class.badge--match]="card.matched" [class.badge--nomatch]="!card.matched">
                  {{ card.matched ? '✓ Identifiée' : '? Non identifiée' }}
                </span>
                <span class="confidence">{{ (card.confidence * 100) | number:'1.0-0' }}% confiance</span>
              </div>
            </div>
          }
        </div>

        <div class="review-actions">
          <button class="btn-ghost" (click)="reset()">🔄 Nouvelle capture</button>
          <button
            class="btn-primary"
            [disabled]="selectedCount() === 0 || adding()"
            (click)="confirmAdd()"
          >
            {{ adding() ? '…' : 'Ajouter ' + selectedCount() + ' carte(s)' }}
          </button>
        </div>
      </div>
    }
  `,
})
export class CollectionScanComponent implements OnInit, OnDestroy {
  private readonly collectionService = inject(CollectionService);
  private readonly toast = inject(ToastService);

  readonly closed = output<void>();
  readonly added = output<number>();

  readonly phase = signal<'camera' | 'analyzing' | 'review'>('camera');
  readonly results = signal<CardScanMatchDto[]>([]);
  readonly selected = signal<boolean[]>([]);
  readonly stableCount = signal(0);
  readonly analyzing = signal(false);
  readonly adding = signal(false);
  readonly cameraError = signal<string | null>(null);

  readonly selectedCount = computed(
    () => this.results().filter((r, i) => r.matched && this.selected()[i]).length
  );

  @ViewChild('videoEl') videoEl!: ElementRef<HTMLVideoElement>;
  @ViewChild('overlayEl') overlayEl!: ElementRef<HTMLCanvasElement>;

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private cv: any;
  private stream: MediaStream | null = null;
  private rafId: number | null = null;
  private stableFrames = 0;
  private detectedRects: DOMRect[] = [];

  ngOnInit(): void {
    const w = window as unknown as { cv?: unknown };
    if (w.cv) {
      this.cv = w.cv;
      void this.startCamera();
      return;
    }

    const existing = document.querySelector<HTMLScriptElement>(`script[src="${OPENCV_CDN}"]`);
    const onReady = () => {
      const globalCv = (window as unknown as { cv: { onRuntimeInitialized?: () => void } }).cv;
      if (globalCv && !this.cv) {
        globalCv['onRuntimeInitialized'] = () => {
          this.cv = globalCv;
          void this.startCamera();
        };
        // Si le runtime est déjà prêt (script mis en cache), démarrer directement.
        if ((globalCv as { Mat?: unknown }).Mat) {
          this.cv = globalCv;
          void this.startCamera();
        }
      }
    };

    if (existing) {
      existing.addEventListener('load', onReady);
      onReady();
    } else {
      const script = document.createElement('script');
      script.src = OPENCV_CDN;
      script.async = true;
      script.onload = onReady;
      script.onerror = () => this.cameraError.set('Impossible de charger le moteur de détection (OpenCV).');
      document.head.appendChild(script);
    }
  }

  ngOnDestroy(): void {
    this.stopCamera();
  }

  async startCamera(): Promise<void> {
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment', width: { ideal: 1920 }, height: { ideal: 1080 } },
      });
      const video = this.videoEl.nativeElement;
      video.srcObject = this.stream;
      video.onloadedmetadata = () => this.startDetectionLoop();
    } catch {
      this.cameraError.set('Accès caméra refusé ou indisponible.');
    }
  }

  private startDetectionLoop(): void {
    const video = this.videoEl.nativeElement;
    const canvas = this.overlayEl.nativeElement;
    const ctx = canvas.getContext('2d');
    const cv = this.cv;
    if (!ctx || !cv) return;

    const detect = () => {
      if (this.phase() !== 'camera') return;

      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      if (!canvas.width || !canvas.height) {
        this.rafId = requestAnimationFrame(detect);
        return;
      }

      ctx.drawImage(video, 0, 0);
      const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);

      const src = new cv.Mat(video.videoHeight, video.videoWidth, cv.CV_8UC4);
      const gray = new cv.Mat();
      const blur = new cv.Mat();
      const edge = new cv.Mat();
      const hier = new cv.Mat();
      const cnts = new cv.MatVector();
      src.data.set(imageData.data);

      cv.cvtColor(src, gray, cv.COLOR_RGBA2GRAY);
      cv.GaussianBlur(gray, blur, new cv.Size(5, 5), 0);
      cv.Canny(blur, edge, 50, 150);
      cv.findContours(edge, cnts, hier, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE);

      const cards: DOMRect[] = [];
      const minArea = canvas.width * canvas.height * 0.03;

      for (let i = 0; i < cnts.size(); i++) {
        const cnt = cnts.get(i);
        const peri = cv.arcLength(cnt, true);
        const approx = new cv.Mat();
        cv.approxPolyDP(cnt, approx, 0.02 * peri, true);

        if (approx.rows === 4) {
          const rect = cv.boundingRect(cnt);
          const area = rect.width * rect.height;
          const ratio = rect.width / rect.height;
          // Aspect ratio carte Pokémon ~0.716 (63mm/88mm) ± tolérance.
          if (area > minArea && ratio > 0.6 && ratio < 0.82) {
            cards.push(new DOMRect(rect.x, rect.y, rect.width, rect.height));
          }
        }
        approx.delete();
        cnt.delete();
      }

      if (cards.length > 0 && cards.length === this.detectedRects.length) {
        this.stableFrames++;
      } else {
        this.stableFrames = 0;
      }
      this.detectedRects = cards;
      const stable = this.stableFrames >= 20;
      this.stableCount.set(stable ? cards.length : 0);

      ctx.clearRect(0, 0, canvas.width, canvas.height);
      for (const r of cards) {
        ctx.strokeStyle = stable ? '#22c55e' : '#ef4444';
        ctx.lineWidth = 3;
        ctx.strokeRect(r.x, r.y, r.width, r.height);
        if (stable) {
          ctx.fillStyle = 'rgba(34,197,94,0.08)';
          ctx.fillRect(r.x, r.y, r.width, r.height);
        }
      }

      src.delete();
      gray.delete();
      blur.delete();
      edge.delete();
      hier.delete();
      cnts.delete();

      this.rafId = requestAnimationFrame(detect);
    };

    this.rafId = requestAnimationFrame(detect);
  }

  capture(): void {
    if (this.analyzing()) return;
    this.analyzing.set(true);
    this.phase.set('analyzing');

    if (this.rafId) cancelAnimationFrame(this.rafId);

    const video = this.videoEl.nativeElement;
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d')?.drawImage(video, 0, 0);

    canvas.toBlob((blob) => {
      if (!blob) {
        this.analyzing.set(false);
        this.phase.set('camera');
        this.startDetectionLoop();
        return;
      }

      const fd = new FormData();
      fd.append('image', blob, 'scan.jpg');

      this.collectionService.scanImage(fd).subscribe({
        next: (result) => {
          this.results.set(result.matches);
          this.selected.set(result.matches.map((m) => m.matched));
          this.phase.set('review');
          this.analyzing.set(false);
        },
        error: () => {
          this.analyzing.set(false);
          this.phase.set('camera');
          this.toast.show({ message: 'Analyse indisponible — Ollama Vision inaccessible.', type: 'danger' });
          this.startDetectionLoop();
        },
      });
    }, 'image/jpeg', 0.92);
  }

  confirmAdd(): void {
    if (this.adding()) return;

    const matched = this.results()
      .map((r, i) => ({ r, i }))
      .filter(({ r, i }) => r.matched && this.selected()[i]);

    const cardIds = matched.map(({ r }) => r.cardId!).filter((id): id is string => id !== null);
    const cards = matched.map(({ r }) => r);

    this.adding.set(true);
    this.collectionService.bulkAddOwned(cardIds, cards).subscribe({
      next: (res) => {
        this.adding.set(false);
        this.toast.show({
          message: `${res.added} carte(s) ajoutée(s)${res.alreadyOwned ? ` · ${res.alreadyOwned} déjà possédée(s)` : ''}.`,
          type: 'success',
        });
        this.added.emit(res.added);
        this.close();
      },
      error: () => {
        this.adding.set(false);
        this.toast.show({ message: 'Erreur lors de l\'ajout.', type: 'danger' });
      },
    });
  }

  toggleSelect(i: number): void {
    this.selected.update((s) => s.map((v, j) => (j === i ? !v : v)));
  }

  reset(): void {
    this.results.set([]);
    this.selected.set([]);
    this.stableFrames = 0;
    this.detectedRects = [];
    this.stableCount.set(0);
    this.phase.set('camera');
    this.startDetectionLoop();
  }

  close(): void {
    this.stopCamera();
    this.closed.emit();
  }

  private stopCamera(): void {
    if (this.rafId) {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
    this.stream?.getTracks().forEach((t) => t.stop());
    this.stream = null;
  }
}
