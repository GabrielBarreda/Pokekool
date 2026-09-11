import {
  Component, OnInit, OnDestroy, inject, signal, computed, ChangeDetectionStrategy, Injector,
} from '@angular/core';
import { NgIf } from '@angular/common';
import { toObservable } from '@angular/core/rxjs-interop';
import { interval, Subscription, distinctUntilChanged, map } from 'rxjs';
import { DashboardStore } from '@features/dashboard/dashboard.store';
import { MarketService } from '@features/bourse/market.service';
import { SyncProgressEvent } from '@features/bourse/market.models';

@Component({
  selector: 'app-sync-progress-overlay',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgIf],
  template: `
    <!-- Sync en cours -->
    <div *ngIf="isRunning()" class="sync-overlay" role="status" aria-live="polite">

      <span class="sync-overlay__badge">{{ badgeText() }}</span>

      <span class="sync-overlay__counter">
        {{ itemsProcessed() }}{{ totalItems() > 0 ? ' / ' + totalItems() : '' }}
      </span>

      <div class="sync-overlay__bar" aria-hidden="true">
        <div
          class="sync-overlay__bar-fill"
          [style.width.%]="totalItems() > 0 ? progress() : null"
          [class.sync-overlay__bar-fill--indeterminate]="totalItems() === 0"
        ></div>
      </div>

      <span *ngIf="totalItems() > 0" class="sync-overlay__percent">
        {{ progress() }}%
      </span>

      <span *ngIf="etaText()" class="sync-overlay__eta">{{ etaText() }}</span>

      <span class="sync-overlay__ticker" [class.sync-overlay__ticker--error]="lastEvent() !== null && !lastEvent()!.success">
        {{ tickerText() }}
      </span>

    </div>

    <!-- Réindexation embeddings DINOv2 (démarrage auto, indépendante du sync prix/pop) -->
    <div *ngIf="embeddingRunning()" class="sync-overlay" role="status" aria-live="polite">

      <span class="sync-overlay__badge">🧠 EMBEDDINGS</span>

      <span class="sync-overlay__counter">
        {{ embeddingProcessed() }} / {{ embeddingTotal() }}
      </span>

      <div class="sync-overlay__bar" aria-hidden="true">
        <div class="sync-overlay__bar-fill" [style.width.%]="embeddingProgress()"></div>
      </div>

      <span class="sync-overlay__percent">{{ embeddingProgress() }}%</span>

      <span class="sync-overlay__ticker">
        {{ embeddingRemaining() }} carte(s) restante(s)
      </span>

    </div>

    <!-- Flash terminé embeddings -->
    <div *ngIf="embeddingShowCompleted()" class="sync-overlay sync-overlay--done" role="status" aria-live="polite">
      🧠 Embeddings à jour — {{ embeddingProcessed() }} carte(s) indexée(s)
    </div>

    <!-- Flash terminé -->
    <div *ngIf="showCompleted()" class="sync-overlay sync-overlay--done" role="status" aria-live="polite">
      ✅ Sync terminée — {{ itemsProcessed() }} cartes
    </div>
  `,
  styles: [`
    .sync-overlay {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      font-size: var(--font-size-xs);
    }

    .sync-overlay--done {
      color: var(--color-semantic-success, #22c55e);
      font-weight: var(--font-weight-medium);
      animation: fadeOut 3s forwards;
      @keyframes fadeOut {
        0%, 70% { opacity: 1; }
        100%     { opacity: 0; }
      }
    }

    .sync-overlay__badge {
      flex-shrink: 0;
      font-size: 10px;
      font-weight: var(--font-weight-semibold);
      letter-spacing: 0.06em;
      color: var(--color-brand-primary);
      text-transform: uppercase;
    }

    .sync-overlay__counter {
      flex-shrink: 0;
      color: var(--color-text-secondary);
      font-variant-numeric: tabular-nums;
    }

    .sync-overlay__bar {
      width: 100px;
      height: 5px;
      background: var(--color-border-subtle);
      border-radius: var(--radius-full);
      overflow: hidden;
      flex-shrink: 0;
    }

    .sync-overlay__bar-fill {
      height: 100%;
      background: var(--color-brand-primary);
      border-radius: var(--radius-full);
      transition: width 0.4s ease;

      &--indeterminate {
        width: 40% !important;
        animation: indeterminate 1.4s ease-in-out infinite;
        @keyframes indeterminate {
          0%   { transform: translateX(-100%); }
          100% { transform: translateX(350%); }
        }
      }
    }

    .sync-overlay__percent {
      flex-shrink: 0;
      color: var(--color-text-muted);
      font-variant-numeric: tabular-nums;
    }

    .sync-overlay__eta {
      flex-shrink: 0;
      color: var(--color-text-muted);
      font-size: 10px;
      font-variant-numeric: tabular-nums;
    }

    .sync-overlay__ticker {
      max-width: 260px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      color: var(--color-text-secondary);

      &--error { color: var(--color-semantic-error, #ef4444); }
    }
  `],
})
export class SyncProgressOverlayComponent implements OnInit, OnDestroy {

  private readonly dashStore = inject(DashboardStore);
  private readonly market    = inject(MarketService);

  // ── State ──────────────────────────────────────────────────────────────────

  readonly lastEvent      = signal<SyncProgressEvent | null>(null);
  readonly itemsProcessed = signal(0);
  readonly totalItems     = signal(0);
  readonly showCompleted  = signal(false);
  readonly syncType       = signal<'prices' | 'population' | 'catalog' | 'embeddings'>('prices');

  readonly isRunning = computed(() => this.dashStore.syncStatus()?.status === 'running');

  // ── Réindexation embeddings DINOv2 (indépendante du sync prix/pop) ───────────
  // Se déclenche automatiquement au démarrage de l'API (EmbeddingReindexHostedService),
  // sans action utilisateur — découverte par polling du statut, pas par un déclenchement UI.
  readonly embeddingRunning = computed(() => this.dashStore.embeddingStatus()?.status === 'running');
  readonly embeddingTotal   = computed(() => this.dashStore.embeddingStatus()?.totalItems ?? 0);
  readonly embeddingRemaining = computed(() => this.dashStore.embeddingStatus()?.remaining ?? 0);
  readonly embeddingProcessed = computed(() => {
    const s = this.dashStore.embeddingStatus();
    return s ? s.itemsProcessed + s.itemsFailed : 0;
  });
  readonly embeddingProgress = computed(() =>
    this.embeddingTotal() > 0
      ? Math.min(100, Math.round(this.embeddingProcessed() / this.embeddingTotal() * 100))
      : 0
  );
  readonly embeddingShowCompleted = signal(false);

  readonly progress = computed(() =>
    this.totalItems() > 0
      ? Math.min(100, Math.round(this.itemsProcessed() / this.totalItems() * 100))
      : 0
  );

  readonly badgeText = computed(() => {
    switch (this.syncType()) {
      case 'population': return '🔬 SYNC POP';
      case 'catalog':    return '📦 SYNC CATALOG';
      default:           return '🔄 SYNC PRIX';
    }
  });

  readonly etaText = computed(() => {
    const processed = this.itemsProcessed();
    const total     = this.totalItems();
    if (processed < 3 || total <= 0) return null;

    const elapsed = Date.now() - this.syncStartTime;
    if (elapsed <= 0) return null;

    const remaining = Math.round((elapsed / processed) * (total - processed) / 1000);
    if (remaining <= 0) return null;

    const min = Math.floor(remaining / 60);
    const sec = remaining % 60;
    return min > 0
      ? `~${min}min ${sec}s restant`
      : `~${sec}s restant`;
  });

  readonly tickerText = computed(() => {
    const e = this.lastEvent();
    if (!e) return 'Connexion au flux...';
    const icon = e.success ? '✅' : '❌';
    // Sync catalogue (par set) : pas de numéro de carte — évite d'afficher un "#" vide.
    const numberPart = e.cardNumber ? ` #${e.cardNumber}` : '';
    return `${e.setName} — ${e.cardName}${numberPart} ${icon}`;
  });

  // ── Subscriptions ──────────────────────────────────────────────────────────

  private sseSub?:       Subscription;
  private pollSub?:      Subscription;
  private statusSub?:    Subscription;
  private completedTimer?: ReturnType<typeof setTimeout>;
  private wasRunning    = false;
  private syncStartTime = 0;

  private embeddingPollSub?:      Subscription;
  private embeddingStatusSub?:    Subscription;
  private embeddingCompletedTimer?: ReturnType<typeof setTimeout>;
  private embeddingWasRunning     = false;

  private readonly injector = inject(Injector);

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Charge le statut immédiatement, puis toutes les 30s
    this.dashStore.loadSyncStatus();
    this.pollSub = interval(30_000).subscribe(() => this.dashStore.loadSyncStatus());

    // Réagit aux changements de statut
    this.statusSub = toObservable(this.dashStore.syncStatus, { injector: this.injector }).pipe(
      map(dto => dto?.status ?? 'none'),
      distinctUntilChanged(),
    ).subscribe(status => {
      if (status === 'running' && !this.wasRunning) {
        this.wasRunning = true;
        this.showCompleted.set(false);
        this.connectSse();
      } else if ((status === 'completed' || status === 'cancelled') && this.wasRunning) {
        this.wasRunning = false;
        this.disconnectSse();
        this.showCompleted.set(true);
        this.completedTimer = setTimeout(() => this.showCompleted.set(false), 3_000);
      }
    });

    // Réindexation embeddings : pas de déclenchement UI (démarrage auto côté API), donc on
    // découvre l'état par polling rapide tant qu'aucune donnée n'est encore chargée/en cours,
    // pour afficher la barre dès le premier chargement de page après un redémarrage backend.
    this.dashStore.loadEmbeddingStatus();
    this.embeddingPollSub = interval(5_000).subscribe(() => this.dashStore.loadEmbeddingStatus());

    this.embeddingStatusSub = toObservable(this.dashStore.embeddingStatus, { injector: this.injector }).pipe(
      map(dto => dto?.status ?? 'idle'),
      distinctUntilChanged(),
    ).subscribe(status => {
      if (status === 'running' && !this.embeddingWasRunning) {
        this.embeddingWasRunning = true;
        this.embeddingShowCompleted.set(false);
      } else if (status === 'completed' && this.embeddingWasRunning) {
        this.embeddingWasRunning = false;
        this.embeddingShowCompleted.set(true);
        this.embeddingCompletedTimer = setTimeout(() => this.embeddingShowCompleted.set(false), 4_000);
      }
    });
  }

  ngOnDestroy(): void {
    this.sseSub?.unsubscribe();
    this.pollSub?.unsubscribe();
    this.statusSub?.unsubscribe();
    if (this.completedTimer) clearTimeout(this.completedTimer);

    this.embeddingPollSub?.unsubscribe();
    this.embeddingStatusSub?.unsubscribe();
    if (this.embeddingCompletedTimer) clearTimeout(this.embeddingCompletedTimer);
  }

  // ── Private ────────────────────────────────────────────────────────────────

  private connectSse(): void {
    this.itemsProcessed.set(0);
    this.totalItems.set(0);
    this.lastEvent.set(null);
    this.syncStartTime = Date.now();
    this.sseSub?.unsubscribe();
    this.sseSub = this.market.connectProgressStream().subscribe(evt => {
      // Mettre à jour le type de sync depuis le premier événement
      if (evt.syncType) this.syncType.set(evt.syncType);
      // Mettre à jour le total si fourni (1er événement population)
      if (evt.totalItems != null && this.totalItems() === 0) {
        this.totalItems.set(evt.totalItems);
      }
      this.lastEvent.set(evt);
      this.itemsProcessed.update(n => n + 1);
    });
  }

  private disconnectSse(): void {
    this.sseSub?.unsubscribe();
    this.sseSub = undefined;
  }
}
