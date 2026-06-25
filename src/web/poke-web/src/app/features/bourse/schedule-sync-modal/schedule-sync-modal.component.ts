import {
  Component, Output, EventEmitter, signal, computed,
  ChangeDetectionStrategy, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MarketService } from '../market.service';
import { SyncScheduleDto } from '../market.models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-schedule-sync-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <!-- Backdrop -->
    <div class="backdrop" (click)="closed.emit()"></div>

    <!-- Modal -->
    <div class="modal" role="dialog" aria-modal="true" aria-label="Programmer un sync prix">
      <div class="modal-header">
        <div class="modal-title">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
          </svg>
          Programmer un sync prix
        </div>
        <button class="close-btn" (click)="closed.emit()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M18 6 6 18M6 6l12 12"/>
          </svg>
        </button>
      </div>

      <!-- Avertissement local -->
      @if (isLocalDev) {
        <div class="local-warning">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
          </svg>
          <div>
            <strong>Environnement local</strong><br>
            Vous devez laisser votre machine en veille (écran éteint, PC actif) jusqu'à la prochaine utilisation.
          </div>
        </div>
      }

      <!-- État actuel si schedule existant -->
      @if (currentSchedule() && currentSchedule()!.status === 'pending') {
        <div class="current-schedule">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
          </svg>
          Sync planifié à
          <strong>{{ currentSchedule()!.scheduledTime }}</strong>
          @if (currentSchedule()!.isRecurring) { (récurrent) }
          — prochain run {{ currentSchedule()!.nextRunAt | date:'dd/MM HH:mm' }}
          <button class="cancel-link" (click)="cancelExisting()">Annuler</button>
        </div>
      }

      <!-- Formulaire -->
      <div class="modal-body">
        <div class="field">
          <label class="field-label">Heure de démarrage</label>
          <input class="time-input"
                 type="time"
                 [(ngModel)]="selectedTime"
                 required />
          <span class="field-hint">Heure locale ({{ timezoneLabel }})</span>
        </div>

        <label class="checkbox-row">
          <input type="checkbox" [(ngModel)]="isRecurring" />
          <span>Répéter chaque nuit à cette heure</span>
        </label>

        @if (nextRunLabel()) {
          <div class="next-run-preview">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
            </svg>
            Prochain lancement : <strong>{{ nextRunLabel() }}</strong>
            @if (isRecurring) { — puis chaque nuit }
          </div>
        }
      </div>

      <div class="modal-footer">
        <button class="btn btn--secondary" (click)="closed.emit()">Annuler</button>
        <button class="btn btn--primary"
                [disabled]="!selectedTime || saving()"
                (click)="confirm()">
          @if (saving()) { Planification… } @else { Planifier le sync }
        </button>
      </div>
    </div>
  `,
  styles: [`
    .backdrop {
      position: fixed; inset: 0; z-index: calc(var(--z-modal) - 1);
      background: var(--color-surface-overlay);
    }
    .modal {
      position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
      z-index: var(--z-modal);
      width: min(480px, 90vw);
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-xl);
      display: flex; flex-direction: column;
    }
    .modal-header {
      display: flex; align-items: center; gap: var(--space-3);
      padding: var(--space-5) var(--space-6);
      border-bottom: 1px solid var(--color-border-default);
    }
    .modal-title {
      display: flex; align-items: center; gap: var(--space-2); flex: 1;
      font-size: var(--font-size-base); font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
      svg { color: var(--color-brand-primary); }
    }
    .close-btn {
      display: flex; align-items: center; justify-content: center;
      width: 30px; height: 30px; border-radius: var(--radius-md);
      border: 1px solid var(--color-border-default); background: transparent;
      color: var(--color-text-muted); cursor: pointer;
      &:hover { background: var(--color-neutral-100); }
    }

    /* Warning local */
    .local-warning {
      display: flex; gap: var(--space-3); align-items: flex-start;
      margin: var(--space-4) var(--space-6) 0;
      padding: var(--space-3) var(--space-4);
      background: var(--color-semantic-warning-subtle);
      border: 1px solid var(--color-semantic-warning);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm); color: #7c5900;
      svg { color: #c47900; flex-shrink: 0; margin-top: 2px; }
      strong { font-weight: var(--font-weight-semibold); }
    }

    /* Schedule existant */
    .current-schedule {
      display: flex; align-items: center; gap: var(--space-2); flex-wrap: wrap;
      margin: var(--space-4) var(--space-6) 0;
      padding: var(--space-3) var(--space-4);
      background: var(--color-semantic-info-subtle);
      border: 1px solid var(--color-semantic-info);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm); color: var(--color-text-secondary);
      svg { color: var(--color-semantic-info); flex-shrink: 0; }
    }
    .cancel-link {
      margin-left: auto; background: none; border: none;
      color: var(--color-semantic-error); font-size: var(--font-size-xs);
      cursor: pointer; text-decoration: underline;
    }

    /* Body */
    .modal-body {
      padding: var(--space-5) var(--space-6);
      display: flex; flex-direction: column; gap: var(--space-4);
    }
    .field { display: flex; flex-direction: column; gap: var(--space-1); }
    .field-label {
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium);
      color: var(--color-text-primary);
    }
    .field-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .time-input {
      width: 160px; padding: var(--space-2) var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-raised); color: var(--color-text-primary);
      font-size: var(--font-size-base);
      &:focus { outline: 2px solid var(--color-brand-primary); border-color: transparent; }
    }
    .checkbox-row {
      display: flex; align-items: center; gap: var(--space-2);
      font-size: var(--font-size-sm); color: var(--color-text-primary); cursor: pointer;
      input[type="checkbox"] { width: 16px; height: 16px; cursor: pointer; accent-color: var(--color-brand-primary); }
    }
    .next-run-preview {
      display: flex; align-items: center; gap: var(--space-2);
      font-size: var(--font-size-sm); color: var(--color-text-secondary);
      svg { color: var(--color-brand-primary); flex-shrink: 0; }
      strong { color: var(--color-text-primary); }
    }

    /* Footer */
    .modal-footer {
      display: flex; justify-content: flex-end; gap: var(--space-3);
      padding: var(--space-4) var(--space-6);
      border-top: 1px solid var(--color-border-default);
    }
    .btn {
      padding: var(--space-2) var(--space-5); border-radius: var(--radius-md);
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium);
      cursor: pointer; border: 1px solid transparent;
      &:disabled { opacity: 0.5; cursor: not-allowed; }
      &--primary {
        background: var(--color-brand-primary); color: white; border-color: var(--color-brand-primary);
        &:hover:not(:disabled) { filter: brightness(1.1); }
      }
      &--secondary {
        background: transparent; border-color: var(--color-border-default);
        color: var(--color-text-primary);
        &:hover { background: var(--color-neutral-100); }
      }
    }
  `],
})
export class ScheduleSyncModalComponent implements OnInit {
  @Output() closed   = new EventEmitter<void>();
  @Output() scheduled = new EventEmitter<SyncScheduleDto>();

  readonly isLocalDev = !environment.production;
  readonly timezoneLabel = Intl.DateTimeFormat().resolvedOptions().timeZone;

  selectedTime = '02:30';
  isRecurring  = true;
  readonly saving = signal(false);
  readonly currentSchedule = signal<SyncScheduleDto | null>(null);

  readonly nextRunLabel = computed(() => {
    if (!this.selectedTime) return null;
    const [h, m] = this.selectedTime.split(':').map(Number);
    const now  = new Date();
    const run  = new Date();
    run.setHours(h, m, 0, 0);
    if (run <= now) run.setDate(run.getDate() + 1);
    return run.toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' });
  });

  constructor(private svc: MarketService) {}

  ngOnInit(): void {
    this.svc.getSchedule().subscribe({
      next: dto => this.currentSchedule.set(dto.status === 'none' ? null : dto),
    });
  }

  cancelExisting(): void {
    this.svc.cancelSchedule().subscribe(() => this.currentSchedule.set(null));
  }

  confirm(): void {
    if (!this.selectedTime) return;
    this.saving.set(true);
    this.svc.scheduleSync(this.selectedTime, this.isRecurring).subscribe({
      next: dto => {
        this.saving.set(false);
        this.scheduled.emit(dto);
        this.closed.emit();
      },
      error: () => this.saving.set(false),
    });
  }
}
