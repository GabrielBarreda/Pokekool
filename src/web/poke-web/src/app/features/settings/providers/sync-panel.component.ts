import {
  Component, OnInit, OnDestroy, ChangeDetectionStrategy,
  signal, computed, inject,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

// ── Types locaux ─────────────────────────────────────────────────────────────

interface ScheduleState {
  scheduledTime: string | null;
  isRecurring: boolean;
  nextRunAt: string | null;
  status: 'none' | 'pending' | 'running' | 'completed' | 'cancelled';
  lastCompletedAt: string | null;
  lastPricesUpserted: number | null;
}

interface PlanEvent {
  kind: 'past' | 'scheduled' | 'running' | 'cancelled';
  date: Date;
  label: string;
  sub?: string;
  recurring?: boolean;
}

// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-sync-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="panel">

      <!-- ── En-tête ── -->
      <div class="panel__head">
        <div class="panel__title">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
          </svg>
          Syncs planifiées
        </div>

        <!-- Bouton toggle form -->
        @if (!showForm()) {
          <button class="panel__add-btn" (click)="showForm.set(true)">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Programmer
          </button>
        }
      </div>

      <!-- ── Timeline ── -->
      <div class="panel__timeline">

        @if (loading()) {
          <div class="panel__empty">Chargement…</div>

        } @else if (events().length === 0) {
          <div class="panel__empty">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/>
              <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
            </svg>
            <span>Aucun sync passé ou planifié</span>
          </div>

        } @else {
          <div class="timeline">
            @for (evt of events(); track $index; let last = $last) {
              <div class="tl-item" [attr.data-kind]="evt.kind">
                <!-- Ligne verticale reliant les points -->
                @if (!last) { <div class="tl-line"></div> }

                <div class="tl-dot"></div>

                <div class="tl-body">
                  <div class="tl-label">
                    {{ evt.label }}
                    @if (evt.recurring) {
                      <span class="tl-recur-tag">récurrent</span>
                    }
                  </div>
                  <div class="tl-date">{{ formatDate(evt.date) }}</div>
                  @if (evt.sub) {
                    <div class="tl-sub">{{ evt.sub }}</div>
                  }
                  <!-- Action Annuler sur sync pending -->
                  @if (evt.kind === 'scheduled') {
                    <button class="tl-cancel-btn" (click)="cancelExisting()">
                      Annuler ce sync
                    </button>
                  }
                </div>
              </div>
            }
          </div>
        }
      </div>

      <!-- ── Formulaire inline (expand) ── -->
      @if (showForm()) {
        <div class="panel__form">
          <div class="form-head">
            <span class="form-head__title">Programmer un sync</span>
            <button class="form-head__close" (click)="showForm.set(false)" aria-label="Fermer">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M18 6 6 18M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <!-- Warning local -->
          @if (isLocalDev) {
            <div class="form-warn">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
                <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
              </svg>
              <span>
                <strong>Environnement local</strong> — laissez le PC actif
                (écran éteint, PC allumé) jusqu'au prochain sync.
              </span>
            </div>
          }

          <!-- Champ heure -->
          <div class="form-field">
            <label class="form-label" for="sync-time">Heure de démarrage</label>
            <div class="form-field__row">
              <input
                id="sync-time"
                class="form-time"
                type="time"
                [(ngModel)]="selectedTime"
              />
              <span class="form-tz">{{ timezone }}</span>
            </div>
          </div>

          <!-- Récurrent -->
          <label class="form-check">
            <input type="checkbox" [(ngModel)]="isRecurring" />
            <span>Répéter chaque nuit à cette heure</span>
          </label>

          <!-- Preview prochain run -->
          @if (nextRunLabel()) {
            <div class="form-preview">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
              </svg>
              Prochain lancement : <strong>{{ nextRunLabel() }}</strong>
              @if (isRecurring) { &nbsp;— puis chaque nuit }
            </div>
          }

          <!-- Boutons -->
          <div class="form-actions">
            <button class="btn btn--ghost" (click)="showForm.set(false)">
              Annuler
            </button>
            <button
              class="btn btn--primary"
              [disabled]="!selectedTime || saving()"
              (click)="confirm()"
            >
              @if (saving()) { Planification… } @else { Planifier le sync }
            </button>
          </div>
        </div>
      }

      <!-- Note prod -->
      @if (!isLocalDev) {
        <p class="panel__prod-note">
          En production, les syncs s'exécutent sur le serveur sans intervention manuelle.
        </p>
      }

    </div>
  `,
  styles: [`
    /* ── Conteneur général ── */
    .panel {
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-sm);
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }

    /* ── En-tête ── */
    .panel__head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: var(--space-4) var(--space-5);
      border-bottom: 1px solid var(--color-border-subtle);
    }

    .panel__title {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
      svg { color: var(--color-brand-primary); }
    }

    .panel__add-btn {
      display: inline-flex;
      align-items: center;
      gap: var(--space-1);
      padding: 4px 10px;
      border-radius: var(--radius-md);
      border: 1px solid var(--color-brand-primary);
      background: var(--color-brand-primary-subtle);
      color: var(--color-brand-primary);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      cursor: pointer;
      transition: background var(--duration-fast) var(--easing-standard);
      &:hover { background: var(--color-brand-primary); color: #fff; }
    }

    /* ── Zone timeline ── */
    .panel__timeline {
      padding: var(--space-5);
      min-height: 120px;
    }

    .panel__empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-4) 0;
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
      text-align: center;
      svg { opacity: 0.35; }
    }

    /* ── Timeline items ── */
    .timeline {
      display: flex;
      flex-direction: column;
      gap: 0;
      position: relative;
    }

    .tl-item {
      display: grid;
      grid-template-columns: 16px 1fr;
      grid-template-rows: auto auto;
      column-gap: var(--space-3);
      position: relative;
    }

    /* Ligne verticale */
    .tl-line {
      grid-column: 1;
      grid-row: 2;
      width: 1px;
      background: var(--color-border-subtle);
      margin: 0 auto;
      min-height: var(--space-4);
      align-self: stretch;
    }

    /* Point coloré */
    .tl-dot {
      grid-column: 1;
      grid-row: 1;
      width: 16px;
      height: 16px;
      border-radius: 50%;
      border: 2px solid transparent;
      flex-shrink: 0;
      margin-top: 2px;
      justify-self: center;
    }

    [data-kind="past"] .tl-dot {
      background: var(--color-semantic-danger-subtle, #fee2e2);
      border-color: var(--color-semantic-danger, #dc2626);
    }
    [data-kind="scheduled"] .tl-dot {
      background: var(--color-semantic-info-subtle, #dbeafe);
      border-color: var(--color-semantic-info, #2563eb);
    }
    [data-kind="running"] .tl-dot {
      background: var(--color-semantic-warning-subtle, #fef9c3);
      border-color: var(--color-semantic-warning, #ca8a04);
      animation: pulse 1.2s ease-in-out infinite;
    }
    [data-kind="cancelled"] .tl-dot {
      background: var(--color-surface-raised);
      border-color: var(--color-border-default);
    }

    @keyframes pulse {
      0%, 100% { box-shadow: 0 0 0 0 rgba(202, 138, 4, .4); }
      50%       { box-shadow: 0 0 0 5px rgba(202, 138, 4, 0); }
    }

    /* Corps de l'item */
    .tl-body {
      grid-column: 2;
      grid-row: 1;
      padding-bottom: var(--space-5);
      display: flex;
      flex-direction: column;
      gap: 3px;
    }

    .tl-item:last-child .tl-body { padding-bottom: 0; }

    .tl-label {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
      display: flex;
      align-items: center;
      gap: var(--space-2);

      [data-kind="past"] &     { color: var(--color-semantic-danger, #dc2626); }
      [data-kind="scheduled"] &{ color: var(--color-semantic-info, #2563eb); }
      [data-kind="running"] &  { color: var(--color-semantic-warning, #ca8a04); }
      [data-kind="cancelled"] &{ color: var(--color-text-muted); }
    }

    .tl-recur-tag {
      font-size: 10px;
      font-weight: var(--font-weight-normal);
      padding: 1px 6px;
      border-radius: var(--radius-full);
      background: var(--color-semantic-info-subtle, #dbeafe);
      color: var(--color-semantic-info, #2563eb);
    }

    .tl-date {
      font-size: var(--font-size-xs);
      font-family: var(--font-family-mono);
      color: var(--color-text-muted);
    }

    .tl-sub {
      font-size: var(--font-size-xs);
      color: var(--color-text-secondary);
    }

    .tl-cancel-btn {
      margin-top: var(--space-1);
      background: none;
      border: none;
      padding: 0;
      font-size: var(--font-size-xs);
      color: var(--color-semantic-danger, #dc2626);
      cursor: pointer;
      text-decoration: underline;
      text-underline-offset: 2px;
      width: fit-content;
    }

    /* ── Formulaire inline ── */
    .panel__form {
      border-top: 1px solid var(--color-border-subtle);
      padding: var(--space-4) var(--space-5);
      background: var(--color-surface-sunken);
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
      animation: slide-down 180ms var(--easing-standard);
    }

    @keyframes slide-down {
      from { opacity: 0; transform: translateY(-6px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    .form-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .form-head__title {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
    }

    .form-head__close {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 26px;
      height: 26px;
      border-radius: var(--radius-md);
      border: 1px solid var(--color-border-default);
      background: transparent;
      color: var(--color-text-muted);
      cursor: pointer;
      &:hover { background: var(--color-neutral-100); }
    }

    /* Warning local */
    .form-warn {
      display: flex;
      align-items: flex-start;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      background: var(--color-semantic-warning-subtle);
      border: 1px solid var(--color-semantic-warning);
      border-radius: var(--radius-md);
      font-size: var(--font-size-xs);
      color: #7c5900;
      svg { color: #c47900; flex-shrink: 0; margin-top: 1px; }
      strong { font-weight: var(--font-weight-semibold); }
    }

    .form-field { display: flex; flex-direction: column; gap: var(--space-1); }
    .form-label {
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-secondary);
    }
    .form-field__row { display: flex; align-items: center; gap: var(--space-2); }

    .form-time {
      padding: var(--space-1) var(--space-2);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      font-size: var(--font-size-sm);
      &:focus { outline: 2px solid var(--color-brand-primary); border-color: transparent; }
    }

    .form-tz {
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
    }

    .form-check {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-sm);
      color: var(--color-text-primary);
      cursor: pointer;
      input[type="checkbox"] {
        width: 14px; height: 14px; cursor: pointer;
        accent-color: var(--color-brand-primary);
      }
    }

    .form-preview {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-xs);
      color: var(--color-text-secondary);
      svg { color: var(--color-brand-primary); flex-shrink: 0; }
      strong { color: var(--color-text-primary); }
    }

    .form-actions {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: var(--space-2);
      padding-top: var(--space-1);
    }

    .btn {
      padding: var(--space-2) var(--space-4);
      border-radius: var(--radius-md);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      cursor: pointer;
      border: 1px solid transparent;
      transition: background var(--duration-fast) var(--easing-standard);
      &:disabled { opacity: .5; cursor: not-allowed; }

      &--primary {
        background: var(--color-brand-primary);
        color: #fff;
        border-color: var(--color-brand-primary);
        &:hover:not(:disabled) { filter: brightness(1.1); }
      }
      &--ghost {
        background: transparent;
        border-color: var(--color-border-default);
        color: var(--color-text-secondary);
        &:hover { background: var(--color-surface-raised); }
      }
    }

    /* ── Note prod ── */
    .panel__prod-note {
      margin: 0;
      padding: var(--space-3) var(--space-5);
      border-top: 1px solid var(--color-border-subtle);
      font-size: var(--font-size-xs);
      color: var(--color-text-muted);
      background: var(--color-surface-sunken);
    }
  `],
})
export class SyncPanelComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);

  readonly loading      = signal(true);
  readonly saving       = signal(false);
  readonly showForm     = signal(false);
  readonly state        = signal<ScheduleState | null>(null);

  readonly isLocalDev = !environment.production;
  readonly timezone   = Intl.DateTimeFormat().resolvedOptions().timeZone;

  selectedTime = '02:30';
  isRecurring  = true;

  /** Timeline events triés chronologiquement */
  readonly events = computed((): PlanEvent[] => {
    const s = this.state();
    if (!s) return [];
    const evts: PlanEvent[] = [];

    if (s.lastCompletedAt) {
      evts.push({
        kind: 'past',
        date: new Date(s.lastCompletedAt),
        label: 'Sync complété',
        sub: s.lastPricesUpserted != null
          ? `${s.lastPricesUpserted.toLocaleString('fr-FR')} prix synchronisés`
          : undefined,
      });
    }

    if (s.status === 'running') {
      evts.push({ kind: 'running', date: new Date(), label: 'Sync en cours…' });
    }

    if (s.status === 'pending' && s.nextRunAt) {
      evts.push({
        kind: 'scheduled',
        date: new Date(s.nextRunAt),
        label: `Sync planifié — ${s.scheduledTime}`,
        recurring: s.isRecurring,
      });
    }

    if (s.status === 'cancelled') {
      evts.push({ kind: 'cancelled', date: new Date(), label: 'Sync annulé' });
    }

    return evts.sort((a, b) => a.date.getTime() - b.date.getTime());
  });

  /** Preview du prochain run selon l'heure saisie */
  readonly nextRunLabel = computed(() => {
    if (!this.selectedTime) return null;
    const [h, m] = this.selectedTime.split(':').map(Number);
    const run = new Date();
    run.setHours(h, m, 0, 0);
    if (run <= new Date()) run.setDate(run.getDate() + 1);
    return run.toLocaleDateString('fr-FR', {
      weekday: 'long', day: 'numeric', month: 'long',
      hour: '2-digit', minute: '2-digit',
    });
  });

  private pollTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.fetch();
    this.pollTimer = setInterval(() => {
      if (this.state()?.status === 'running') this.fetch();
    }, 30_000);
  }

  ngOnDestroy(): void {
    if (this.pollTimer) clearInterval(this.pollTimer);
  }

  cancelExisting(): void {
    this.http.delete<ScheduleState>('/api/v1/admin/sync/schedule').subscribe({
      next: s => this.state.set(s),
    });
  }

  confirm(): void {
    if (!this.selectedTime) return;
    this.saving.set(true);
    this.http.post<ScheduleState>('/api/v1/admin/sync/schedule', {
      time: this.selectedTime,
      isRecurring: this.isRecurring,
    }).subscribe({
      next: s => {
        this.state.set(s);
        this.saving.set(false);
        this.showForm.set(false);
      },
      error: () => this.saving.set(false),
    });
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
    const time  = d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
    const date  = d.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' });

    if (diff === 0)  return `Aujourd'hui à ${time}`;
    if (diff === -1) return `Hier à ${time}`;
    if (diff === 1)  return `Demain à ${time}`;
    return `${date} à ${time}`;
  }
}
