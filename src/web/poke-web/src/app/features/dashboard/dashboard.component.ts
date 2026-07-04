import { Component, OnInit, OnDestroy, inject, ChangeDetectionStrategy, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { CardComponent } from '@shared/ui/card/card.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { DashboardStore } from './dashboard.store';
import { RecentlyScrapedWidgetComponent } from './widgets/recently-scraped-widget.component';
import { ForSaleWidgetComponent } from './widgets/for-sale-widget.component';
import { RecentSalesWidgetComponent } from './widgets/recent-sales-widget.component';
import { MessagesWidgetComponent } from './widgets/messages-widget.component';
import { AiStatsWidgetComponent } from './widgets/ai-stats-widget.component';
import { NewItemsWidgetComponent } from './widgets/new-items-widget.component';
import { AiStatsDto } from './dashboard.models';

const AUTO_REFRESH_MS = 60_000;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    PageHeaderComponent,
    CardComponent,
    SpinnerComponent,
    RecentlyScrapedWidgetComponent,
    ForSaleWidgetComponent,
    RecentSalesWidgetComponent,
    MessagesWidgetComponent,
    AiStatsWidgetComponent,
    NewItemsWidgetComponent,
  ],
  template: `
    <app-page-header title="Tableau de bord">
      <div class="header-right">
        @if (isSyncRunning()) {
          <div class="sync-indicator">
            <span class="sync-indicator__dot"></span>
            Sync en cours
          </div>
        }
        @if (store.lastRefreshedAt()) {
          <span class="refresh-ts">
            Mis à jour {{ store.lastRefreshedAt() | date:'HH:mm:ss' }}
          </span>
        }
        <button
          class="btn-refresh"
          (click)="store.refresh()"
          [disabled]="store.loading()"
          aria-label="Rafraîchir le tableau de bord"
        >
          <span [class.spinning]="store.loading()">↻</span>
          Rafraîchir
        </button>
      </div>
    </app-page-header>

    @if (store.loading() && !store.summary()) {
      <div class="loading-center"><app-spinner /></div>
    } @else if (store.error() && !store.summary()) {
      <div class="error-msg">{{ store.error() }}</div>
    } @else {
      <div class="dash-layout">

        <!-- ── Sidebar KPIs ──────────────────────────────────── -->
        <aside class="dash-sidebar">

          <!-- Collectionneurs actifs -->
          <app-card>
            <div class="kpi-widget">
              <span class="kpi-widget__icon">👥</span>
              <div class="kpi-widget__body">
                <span class="kpi-widget__label">Collection</span>
                <span class="kpi-widget__value">{{ store.summary()?.totalUsersCollecting ?? 0 }}</span>
                <span class="kpi-widget__sub">utilisateurs collectionnent</span>
              </div>
            </div>
          </app-card>

          <!-- Nouvelles cartes TCG -->
          <app-new-items-widget
            title="Nouvelles cartes"
            icon="🆕"
            [count]="newCardsCount()"
            [items]="[]"
            emptyMessage="Pas de nouvelle carte récupérée durant la dernière synchronisation avec le catalogue Pokémon."
            linkLabel="Voir dans la bourse"
            linkRoute="/bourse"
            [loading]="isSyncRunning() && newCardsCount() === null"
          />

          <!-- Messages -->
          <app-messages-widget
            [pendingCount]="store.summary()?.pendingMessagesCount ?? 0"
          />

          <!-- IA -->
          <app-ai-stats-widget
            [stats]="store.summary()?.aiStats ?? defaultAiStats"
          />

        </aside>

        <!-- ── Zone principale ───────────────────────────────── -->
        <main class="dash-main">

          <app-recently-scraped-widget
            [cards]="store.summary()?.recentlyScrapedCards ?? []"
            [isSyncRunning]="isSyncRunning()"
          />

          <app-for-sale-widget
            [cards]="store.summary()?.cardsForSale ?? []"
          />

          <app-recent-sales-widget
            [sales]="store.summary()?.recentSales ?? []"
          />

        </main>

      </div>
    }
  `,
  styles: [`
    .header-right {
      display: flex; align-items: center; gap: var(--space-3);
    }
    .refresh-ts {
      font-size: var(--font-size-xs); color: var(--color-text-muted);
    }
    /* ── Sync indicator ───────────────────────────────── */
    .sync-indicator {
      display: flex; align-items: center; gap: var(--space-2);
      font-size: var(--font-size-xs);
      color: var(--color-brand-primary);
      font-weight: var(--font-weight-medium);
    }
    .sync-indicator__dot {
      width: 8px; height: 8px; border-radius: 50%;
      background: var(--color-brand-primary);
      animation: pulse 1.2s ease-in-out infinite;
    }
    @keyframes pulse { 0%, 100% { opacity: 1; transform: scale(1); } 50% { opacity: 0.4; transform: scale(0.8); } }

    .btn-refresh {
      display: inline-flex; align-items: center; gap: var(--space-1);
      padding: var(--space-2) var(--space-3);
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      color: var(--color-text-primary);
      cursor: pointer;
      transition: background 0.15s;
      &:hover:not(:disabled) { background: var(--color-neutral-100); }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
    .spinning {
      display: inline-block;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .loading-center {
      display: flex; justify-content: center; padding: var(--space-16);
    }
    .error-msg {
      text-align: center; padding: var(--space-8);
      color: var(--color-semantic-danger); font-size: var(--font-size-sm);
    }

    /* ── Layout ────────────────────────────────────────── */
    .dash-layout {
      display: grid;
      grid-template-columns: 280px 1fr;
      gap: var(--space-5);
      align-items: start;
    }
    .dash-sidebar {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
      position: sticky;
      top: var(--space-4);
    }
    .dash-main {
      display: flex;
      flex-direction: column;
      gap: var(--space-5);
      min-width: 0;
    }

    /* ── KPI widget (sidebar) ───────────────────────────── */
    .kpi-widget {
      display: flex; gap: var(--space-3); align-items: flex-start;
    }
    .kpi-widget__icon { font-size: 24px; line-height: 1; flex-shrink: 0; }
    .kpi-widget__body { display: flex; flex-direction: column; gap: 2px; }
    .kpi-widget__label {
      font-size: var(--font-size-xs); color: var(--color-text-muted);
      font-weight: var(--font-weight-medium); text-transform: uppercase; letter-spacing: 0.05em;
    }
    .kpi-widget__value {
      font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary); line-height: 1;
    }
    .kpi-widget__sub { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    /* ── Responsive ─────────────────────────────────────── */
    @media (max-width: 768px) {
      .dash-layout {
        grid-template-columns: 1fr;
      }
      .dash-sidebar {
        position: static;
        flex-direction: row;
        flex-wrap: wrap;
      }
      .dash-sidebar > * { flex: 1 1 200px; }
    }
  `],
})
export class DashboardComponent implements OnInit, OnDestroy {
  readonly store = inject(DashboardStore);

  readonly defaultAiStats: AiStatsDto = {
    messagesAnsweredToday:    0,
    messagesAnsweredThisWeek: 0,
    isEnabled:               false,
  };

  /** true si le backend indique un sync en cours */
  readonly isSyncRunning = computed(
    () => this.store.syncStatus()?.status === 'running'
  );

  /**
   * Nombre de nouvelles cartes depuis la dernière sync catalogue.
   * null = données pas encore disponibles (endpoint backend à venir).
   */
  readonly newCardsCount = computed<number | null>(() => null);

  private autoRefreshTimer: ReturnType<typeof setInterval> | null = null;
  private syncPollTimer:    ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.store.loadSummary();
    this.store.loadSyncStatus();

    // Poll le statut de sync toutes les 30s
    this.syncPollTimer = setInterval(() => this.store.loadSyncStatus(), 30_000);

    // Auto-refresh du summary toutes les 60s si sync en cours
    this.autoRefreshTimer = setInterval(() => {
      if (this.isSyncRunning()) this.store.loadSummary();
    }, AUTO_REFRESH_MS);
  }

  ngOnDestroy(): void {
    if (this.autoRefreshTimer) clearInterval(this.autoRefreshTimer);
    if (this.syncPollTimer)    clearInterval(this.syncPollTimer);
  }
}
