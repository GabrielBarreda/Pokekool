import {
  Component, OnInit, OnDestroy, ChangeDetectionStrategy,
  signal, computed, inject,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface ScheduleState {
  scheduledTime: string | null;
  isRecurring: boolean;
  nextRunAt: string | null;
  status: 'none' | 'pending' | 'running' | 'completed' | 'cancelled';
  lastCompletedAt: string | null;
  lastPricesUpserted: number | null;
}

interface PlanningEvent {
  kind: 'past' | 'scheduled' | 'running';
  date: Date;
  label: string;
  sub?: string;
}

@Component({
  selector: 'app-sync-planning',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="planning">
      <div class="planning__header">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
          <line x1="16" y1="2" x2="16" y2="6"/>
          <line x1="8" y1="2" x2="8" y2="6"/>
          <line x1="3" y1="10" x2="21" y2="10"/>
        </svg>
        Historique &amp; planning
      </div>

      @if (loading()) {
        <div class="planning__empty">Chargement…</div>
      } @else if (events().length === 0) {
        <div class="planning__empty">Aucun sync passé ou planifié.</div>
      } @else {
        <div class="planning__timeline">
          @for (evt of events(); track $index) {
            <div class="planning__item" [attr.data-kind]="evt.kind">
              <div class="planning__dot"></div>
              <div class="planning__content">
                <span class="planning__label">{{ evt.label }}</span>
                <span class="planning__date">{{ formatDate(evt.date) }}</span>
                @if (evt.sub) {
                  <span class="planning__sub">{{ evt.sub }}</span>
                }
              </div>
            </div>
          }
        </div>
      }

      @if (!isLocalDev) {
        <div class="planning__hint">
          En production, les syncs tournent sur le serveur sans intervention manuelle.
        </div>
      }
    </div>
  `,
  styles: [`
    .planning {
      margin-top: var(--space-5);
      padding: var(--space-4) var(--space-5);
      background: var(--color-surface-sunken);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-lg);
    }

    .planning__header {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-muted);
      text-transform: uppercase;
      letter-spacing: 0.06em;
      margin-bottom: var(--space-4);
      svg { flex-shrink: 0; }
    }

    .planning__empty {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      padding: var(--space-2) 0;
    }

    /* Timeline */
    .planning__timeline {
      display: flex;
      flex-direction: column;
      gap: 0;
      position: relative;

      /* trait vertical */
      &::before {
        content: '';
        position: absolute;
        left: 7px;
        top: 8px;
        bottom: 8px;
        width: 1px;
        background: var(--color-border-subtle);
      }
    }

    .planning__item {
      display: flex;
      align-items: flex-start;
      gap: var(--space-3);
      padding: var(--space-2) 0;
      position: relative;
    }

    /* Dot coloré selon kind */
    .planning__dot {
      width: 15px;
      height: 15px;
      border-radius: 50%;
      flex-shrink: 0;
      margin-top: 2px;
      border: 2px solid transparent;
      position: relative;
      z-index: 1;
    }

    [data-kind="past"] .planning__dot {
      background: var(--color-semantic-danger-subtle, #fee2e2);
      border-color: var(--color-semantic-danger, #dc2626);
    }

    [data-kind="scheduled"] .planning__dot {
      background: var(--color-semantic-info-subtle, #dbeafe);
      border-color: var(--color-semantic-info, #2563eb);
    }

    [data-kind="running"] .planning__dot {
      background: var(--color-semantic-warning-subtle, #fef9c3);
      border-color: var(--color-semantic-warning, #ca8a04);
      animation: pulse 1.2s ease-in-out infinite;
    }

    @keyframes pulse {
      0%, 100% { box-shadow: 0 0 0 0 rgba(202, 138, 4, 0.4); }
      50%       { box-shadow: 0 0 0 5px rgba(202, 138, 4, 0); }
    }

    .planning__content {
      display: flex;
      flex-direction: column;
      gap: 1px;
      min-width: 0;
    }

    .planning__label {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-primary);
    }

    [data-kind="past"] .planning__label    { color: var(--color-semantic-danger, #dc2626); }
    [data-kind="scheduled"] .planning__label { color: var(--color-semantic-info, #2563eb); }
    [data-kind="running"] .planning__label  { color: var(--color-semantic-warning, #ca8a04); }

    .planning__date {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      font-family: var(--font-family-mono);
    }

    .planning__sub {
      font-size: var(--font-size-xs);
      color: var(--color-text-secondary);
    }

    .planning__hint {
      margin-top: var(--space-3);
      padding-top: var(--space-3);
      border-top: 1px solid var(--color-border-subtle);
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
    }
  `],
})
export class SyncPlanningComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);

  readonly loading = signal(true);
  readonly state   = signal<ScheduleState | null>(null);

  readonly isLocalDev = !environment.production;

  /** Liste ordonnée des events à afficher (passé → futur) */
  readonly events = computed((): PlanningEvent[] => {
    const s = this.state();
    if (!s) return [];

    const evts: PlanningEvent[] = [];

    // Dernier sync complété → rouge
    if (s.lastCompletedAt) {
      evts.push({
        kind: 'past',
        date: new Date(s.lastCompletedAt),
        label: 'Sync complété',
        sub: s.lastPricesUpserted != null
          ? `${s.lastPricesUpserted} prix synchronisés`
          : undefined,
      });
    }

    // En cours → orange
    if (s.status === 'running') {
      evts.push({
        kind: 'running',
        date: new Date(),
        label: 'Sync en cours…',
      });
    }

    // Planifié → bleu
    if (s.status === 'pending' && s.nextRunAt) {
      evts.push({
        kind: 'scheduled',
        date: new Date(s.nextRunAt),
        label: s.isRecurring
          ? `Sync planifié — récurrent à ${s.scheduledTime}`
          : `Sync planifié à ${s.scheduledTime}`,
        sub: s.isRecurring ? 'Répète chaque nuit à cette heure' : undefined,
      });
    }

    // Tri chronologique : passé d'abord
    evts.sort((a, b) => a.date.getTime() - b.date.getTime());
    return evts;
  });

  private pollTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.fetch();
    // Rafraîchit toutes les 30s si running
    this.pollTimer = setInterval(() => {
      if (this.state()?.status === 'running') this.fetch();
    }, 30_000);
  }

  ngOnDestroy(): void {
    if (this.pollTimer) clearInterval(this.pollTimer);
  }

  private fetch(): void {
    this.http.get<ScheduleState>('/api/v1/admin/sync/schedule').subscribe({
      next: s  => { this.state.set(s); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  formatDate(d: Date): string {
    const now   = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const day   = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const diff  = Math.round((day.getTime() - today.getTime()) / 86_400_000);

    const timeStr = d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
    const dateStr = d.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' });

    if (diff === 0)  return `Aujourd'hui à ${timeStr}`;
    if (diff === -1) return `Hier à ${timeStr}`;
    if (diff === 1)  return `Demain à ${timeStr}`;
    return `${dateStr} à ${timeStr}`;
  }
}
