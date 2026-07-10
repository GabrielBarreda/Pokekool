import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ClientService } from '../../client.service';
import { AvailableSlotDto, ClientServiceType } from '../../client.models';
import { ToastService } from '@shared/services/toast.service';

@Component({
  selector: 'app-rdv-new',
  standalone: true,
  imports: [FormsModule],
  template: `
    <header class="page-head">
      <h1 class="page-head__title">Prendre un rendez-vous</h1>
      <p class="page-head__sub">Réservez un créneau pour une prestation en boutique.</p>
    </header>

    <div class="form-card">
      <!-- 1. Service -->
      <div class="step">
        <label class="label">1. Prestation</label>
        <div class="radios">
          <label class="radio" [class.is-active]="service() === 'pre_grading'">
            <input type="radio" name="svc" value="pre_grading" [ngModel]="service()" (ngModelChange)="onService($event)" />
            Pré-gradation
          </label>
          <label class="radio" [class.is-active]="service() === 'restoration'">
            <input type="radio" name="svc" value="restoration" [ngModel]="service()" (ngModelChange)="onService($event)" />
            Restauration
          </label>
        </div>
      </div>

      <!-- 2. Date -->
      <div class="step">
        <label class="label" for="date">2. Date</label>
        <input id="date" class="input" type="date" [min]="minDate" [ngModel]="date()" (ngModelChange)="onDate($event)" />
      </div>

      <!-- 3. Créneau -->
      <div class="step">
        <label class="label">3. Créneau</label>
        @if (!date()) {
          <p class="muted">Choisissez d'abord une date.</p>
        } @else if (loadingSlots()) {
          <p class="muted">Chargement des créneaux…</p>
        } @else if (slots().length === 0) {
          <p class="muted">Aucun créneau disponible ce jour-là.</p>
        } @else {
          <div class="slots">
            @for (s of slots(); track s.time) {
              <label class="slot" [class.is-active]="selectedTime() === s.time" [class.is-disabled]="!s.available">
                <input type="radio" name="slot" [value]="s.time" [disabled]="!s.available"
                       [ngModel]="selectedTime()" (ngModelChange)="selectedTime.set($event)" />
                {{ s.time }}
              </label>
            }
          </div>
        }
      </div>

      <!-- 4. Notes -->
      <div class="step">
        <label class="label" for="notes">4. Notes (optionnel)</label>
        <textarea id="notes" class="textarea" rows="3" placeholder="Précisez votre demande…" [(ngModel)]="notes"></textarea>
        <p class="muted small">Vos nom et email sont automatiquement issus de votre compte.</p>
      </div>

      <button type="button" class="btn btn--primary" [disabled]="!canConfirm() || submitting()" (click)="confirm()">
        Confirmer le rendez-vous
      </button>
    </div>
  `,
  styles: [`
    .page-head { margin-bottom: var(--space-5); }
    .page-head__title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); margin: 0; }
    .page-head__sub { color: var(--color-text-muted); margin: var(--space-1) 0 0; }

    .form-card { max-width: 560px; background: var(--color-surface-base); border: 1px solid var(--color-border-subtle); border-radius: var(--radius-lg); padding: var(--space-6); display: flex; flex-direction: column; gap: var(--space-5); }
    .step { display: flex; flex-direction: column; gap: var(--space-2); }
    .label { font-weight: var(--font-weight-semibold); font-size: var(--font-size-sm); }

    .radios { display: flex; gap: var(--space-3); flex-wrap: wrap; }
    .radio, .slot { border: 1px solid var(--color-border-default); border-radius: var(--radius-md); padding: var(--space-2) var(--space-3); font-size: var(--font-size-sm); cursor: pointer; display: flex; align-items: center; gap: var(--space-2); }
    .radio.is-active, .slot.is-active { border-color: var(--color-brand-primary); color: var(--color-brand-primary); background: var(--color-surface-sunken); }
    .radio input, .slot input { accent-color: var(--color-brand-primary); }

    .input { height: 40px; padding: 0 var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); color: var(--color-text-primary); font-size: var(--font-size-sm); max-width: 220px; }
    .slots { display: flex; flex-wrap: wrap; gap: var(--space-2); }
    .slot.is-disabled { opacity: 0.4; cursor: not-allowed; text-decoration: line-through; }

    .textarea { width: 100%; padding: var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); font-family: inherit; font-size: var(--font-size-sm); resize: vertical; background: var(--color-surface-base); color: var(--color-text-primary); }
    .btn { padding: var(--space-3) var(--space-5); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); cursor: pointer; font-size: var(--font-size-sm); align-self: flex-start; }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn--primary { background: var(--color-brand-primary); color: #fff; border-color: var(--color-brand-primary); }
    .muted { color: var(--color-text-muted); font-size: var(--font-size-sm); }
    .small { font-size: var(--font-size-xs); }
  `],
})
export class RdvNewComponent implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly minDate = new Date().toISOString().slice(0, 10);

  readonly service_ = signal<ClientServiceType | null>(null);
  readonly date = signal('');
  readonly slots = signal<AvailableSlotDto[]>([]);
  readonly loadingSlots = signal(false);
  readonly selectedTime = signal<string | null>(null);
  readonly submitting = signal(false);
  notes = '';

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const svc = params.get('service') as ClientServiceType | null;
    const cardName = params.get('cardName');
    if (svc === 'pre_grading' || svc === 'restoration') {
      this.service_.set(svc);
    }
    if (cardName) {
      this.notes = `Carte : ${cardName}`;
    }
  }

  service(): ClientServiceType | null { return this.service_(); }

  onService(v: ClientServiceType): void {
    this.service_.set(v);
    this.selectedTime.set(null);
    if (this.date()) this.loadSlots();
  }
  onDate(v: string): void {
    this.date.set(v);
    this.selectedTime.set(null);
    if (this.service_()) this.loadSlots();
  }

  private loadSlots(): void {
    const svc = this.service_(); const d = this.date();
    if (!svc || !d) return;
    this.loadingSlots.set(true);
    this.clientService.getAvailableSlots(svc, d).subscribe({
      next: s => { this.slots.set(s); this.loadingSlots.set(false); },
      error: () => { this.slots.set([]); this.loadingSlots.set(false); },
    });
  }

  canConfirm(): boolean {
    return !!this.service_() && !!this.date() && !!this.selectedTime();
  }

  confirm(): void {
    const svc = this.service_(); const d = this.date(); const t = this.selectedTime();
    if (!svc || !d || !t) return;

    // Combine date + heure en ISO local
    const scheduledAt = new Date(`${d}T${t}:00`).toISOString();

    this.submitting.set(true);
    this.clientService.createAppointment({ serviceType: svc, scheduledAt, notesClient: this.notes.trim() || null }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.toast.show({ message: 'Rendez-vous confirmé !', type: 'success' });
        this.router.navigate(['/client/rdv']);
      },
      error: () => {
        this.submitting.set(false);
        this.toast.show({ message: 'Échec de la réservation. Réessayez.', type: 'danger' });
      },
    });
  }
}
