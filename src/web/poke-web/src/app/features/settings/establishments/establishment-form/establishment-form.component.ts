import {
  Component,
  input,
  output,
  inject,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { EstablishmentDto, CreateEstablishmentRequest, UpdateEstablishmentRequest } from '../establishment.models';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';

@Component({
  selector: 'app-establishment-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, SpinnerComponent],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="form">

      <div class="form-group">
        <label class="label" for="ef-name">Nom <span class="required">*</span></label>
        <input id="ef-name" class="input" [class.input--error]="touched('name')"
               formControlName="name" placeholder="Ex : Restaurant du Centre" />
        @if (touched('name')) {
          <span class="error-msg">Le nom est obligatoire (max 200 caractères)</span>
        }
      </div>

      <div class="form-row">
        <div class="form-group">
          <label class="label" for="ef-phone">Téléphone</label>
          <input id="ef-phone" class="input" formControlName="phone" type="tel" placeholder="+33 1 23 45 67 89" />
        </div>
        <div class="form-group">
          <label class="label" for="ef-email">Email</label>
          <input id="ef-email" class="input" [class.input--error]="touched('email')"
                 formControlName="email" type="email" placeholder="contact@restaurant.fr" />
          @if (touched('email')) { <span class="error-msg">Adresse email invalide</span> }
        </div>
      </div>

      <fieldset formGroupName="address" class="fieldset">
        <legend class="fieldset-legend">Adresse</legend>
        <div class="form-group">
          <label class="label" for="ef-l1">Ligne 1</label>
          <input id="ef-l1" class="input" formControlName="line1" placeholder="12 rue de la Paix" />
        </div>
        <div class="form-group">
          <label class="label" for="ef-l2">Ligne 2</label>
          <input id="ef-l2" class="input" formControlName="line2" placeholder="Bâtiment B" />
        </div>
        <div class="form-row">
          <div class="form-group">
            <label class="label" for="ef-cp">Code postal</label>
            <input id="ef-cp" class="input" formControlName="postalCode" placeholder="75001" />
          </div>
          <div class="form-group">
            <label class="label" for="ef-city">Ville</label>
            <input id="ef-city" class="input" formControlName="city" placeholder="Paris" />
          </div>
          <div class="form-group">
            <label class="label" for="ef-country">Pays</label>
            <input id="ef-country" class="input" formControlName="country" placeholder="France" />
          </div>
        </div>
      </fieldset>

      @if (mode() === 'edit') {
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Paramètres réservation</legend>
          <div class="form-row">
            <div class="form-group">
              <label class="label" for="ef-dur">Durée par défaut (min)</label>
              <input id="ef-dur" class="input" formControlName="defaultReservationDurationMinutes" type="number" min="15" max="480" step="15" />
            </div>
            <div class="form-group">
              <label class="label" for="ef-slot">Intervalle créneau (min)</label>
              <input id="ef-slot" class="input" formControlName="reservationSlotIntervalMinutes" type="number" min="5" max="120" step="5" />
            </div>
            <div class="form-group">
              <label class="label" for="ef-win">Fenêtre de résa (jours)</label>
              <input id="ef-win" class="input" formControlName="reservationAdvanceWindowDays" type="number" min="1" max="365" />
            </div>
          </div>
          <div class="form-group">
            <label class="toggle">
              <input type="checkbox" class="toggle-chk" formControlName="isActive" />
              <span class="toggle-track"></span>
              <span class="toggle-text">Établissement actif</span>
            </label>
          </div>
        </fieldset>
      }

      <div class="actions">
        <button type="button" class="btn btn--ghost" (click)="cancelled.emit()">Annuler</button>
        <button type="submit" class="btn btn--primary" [disabled]="saving()">
          @if (saving()) { <app-spinner size="sm" /> }
          {{ mode() === 'create' ? "Créer l'établissement" : 'Enregistrer' }}
        </button>
      </div>
    </form>
  `,
  styles: [`
    .form { display: flex; flex-direction: column; gap: var(--space-5); }
    .form-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px,1fr)); gap: var(--space-4); }
    .form-group { display: flex; flex-direction: column; gap: var(--space-1); }

    .label { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-secondary); }
    .required { color: var(--color-semantic-danger); }

    .input {
      height: 38px;
      padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      font-size: var(--font-size-sm);
      outline: none;
      transition: border-color var(--duration-fast);
      &:focus { border-color: var(--color-brand-primary); box-shadow: 0 0 0 3px var(--color-brand-primary-subtle); }
      &--error { border-color: var(--color-semantic-danger); }
    }
    .error-msg { font-size: var(--font-size-xs); color: var(--color-semantic-danger); }

    .fieldset { border: 1px solid var(--color-border-subtle); border-radius: var(--radius-md); padding: var(--space-4); display: flex; flex-direction: column; gap: var(--space-4); }
    .fieldset-legend { padding: 0 var(--space-2); font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold); color: var(--color-text-muted); text-transform: uppercase; letter-spacing: 0.05em; }

    /* Toggle */
    .toggle { display: flex; align-items: center; gap: var(--space-3); cursor: pointer; }
    .toggle-chk { display: none; }
    .toggle-track {
      position: relative; width: 44px; height: 24px;
      background: var(--color-border-default); border-radius: var(--radius-full);
      transition: background var(--duration-fast);
      &::after {
        content: ''; position: absolute; top: 3px; left: 3px;
        width: 18px; height: 18px;
        background: white; border-radius: 50%;
        transition: transform var(--duration-fast);
        box-shadow: 0 1px 3px rgba(0,0,0,0.2);
      }
    }
    .toggle-chk:checked ~ .toggle-track { background: var(--color-brand-primary); }
    .toggle-chk:checked ~ .toggle-track::after { transform: translateX(20px); }
    .toggle-text { font-size: var(--font-size-sm); color: var(--color-text-primary); }

    .actions {
      display: flex; justify-content: flex-end; gap: var(--space-3);
      padding-top: var(--space-4); border-top: 1px solid var(--color-border-subtle);
    }
    .btn {
      display: inline-flex; align-items: center; gap: var(--space-2);
      height: 38px; padding: 0 var(--space-5);
      border-radius: var(--radius-md); font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium); cursor: pointer; border: none;
      transition: opacity var(--duration-fast);
      &:disabled { opacity: 0.55; cursor: not-allowed; }
    }
    .btn--ghost {
      border: 1px solid var(--color-border-default);
      background: transparent; color: var(--color-text-secondary);
      &:hover:not(:disabled) { background: var(--color-neutral-100); }
    }
    .btn--primary {
      background: var(--color-brand-primary); color: var(--color-brand-on-primary);
      &:hover:not(:disabled) { background: var(--color-brand-primary-hover); }
    }
  `],
})
export class EstablishmentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  readonly mode = input<'create' | 'edit'>('create');
  readonly initialData = input<EstablishmentDto | null>(null);
  readonly saving = input<boolean>(false);

  readonly submitted = output<CreateEstablishmentRequest | UpdateEstablishmentRequest>();
  readonly cancelled = output<void>();

  form!: FormGroup;

  ngOnInit(): void {
    const d = this.initialData();
    this.form = this.fb.group({
      name: [d?.name ?? '', [Validators.required, Validators.maxLength(200)]],
      phone: [d?.phone ?? ''],
      email: [d?.email ?? '', [Validators.email]],
      address: this.fb.group({
        line1: [d?.address?.line1 ?? ''],
        line2: [d?.address?.line2 ?? ''],
        postalCode: [d?.address?.postalCode ?? ''],
        city: [d?.address?.city ?? ''],
        country: [d?.address?.country ?? ''],
      }),
      defaultReservationDurationMinutes: [d?.defaultReservationDurationMinutes ?? 90],
      reservationSlotIntervalMinutes: [d?.reservationSlotIntervalMinutes ?? 30],
      reservationAdvanceWindowDays: [d?.reservationAdvanceWindowDays ?? 30],
      isActive: [d?.isActive ?? true],
    });
  }

  touched(ctrl: string): boolean {
    const c = this.form.get(ctrl);
    return !!(c?.invalid && (c.dirty || c.touched));
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const address = {
      line1: v.address.line1 || null,
      line2: v.address.line2 || null,
      postalCode: v.address.postalCode || null,
      city: v.address.city || null,
      country: v.address.country || null,
    };
    if (this.mode() === 'create') {
      this.submitted.emit({ name: v.name, address, phone: v.phone || null, email: v.email || null, openingHoursJson: null } satisfies CreateEstablishmentRequest);
    } else {
      this.submitted.emit({
        name: v.name, address, phone: v.phone || null, email: v.email || null,
        openingHoursJson: this.initialData()?.openingHoursJson ?? null,
        defaultReservationDurationMinutes: v.defaultReservationDurationMinutes,
        reservationSlotIntervalMinutes: v.reservationSlotIntervalMinutes,
        reservationAdvanceWindowDays: v.reservationAdvanceWindowDays,
        isActive: v.isActive,
      } satisfies UpdateEstablishmentRequest);
    }
  }
}
