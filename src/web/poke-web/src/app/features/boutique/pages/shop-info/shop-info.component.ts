import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ShopInfoStore } from '../../shop-info.store';
import { UpdateShopInfoRequest, WeekHours, DayHours } from '../../boutique.models';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { ShopMapComponent } from './shop-map.component';

const DAYS: { key: string; label: string }[] = [
  { key: 'lundi', label: 'Lundi' },
  { key: 'mardi', label: 'Mardi' },
  { key: 'mercredi', label: 'Mercredi' },
  { key: 'jeudi', label: 'Jeudi' },
  { key: 'vendredi', label: 'Vendredi' },
  { key: 'samedi', label: 'Samedi' },
  { key: 'dimanche', label: 'Dimanche' },
];

const ALL_SERVICES: { key: string; label: string }[] = [
  { key: 'vente', label: 'Vente' },
  { key: 'pre_grading', label: 'Pré-grading' },
  { key: 'restoration', label: 'Restauration' },
  { key: 'rachat', label: 'Rachat' },
];

@Component({
  selector: 'app-shop-info',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, SpinnerComponent, ShopMapComponent],
  template: `
    <div class="page">
      <app-page-header title="Infos boutique" subtitle="Coordonnées, horaires et services de votre boutique physique" />

      @if (store.loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else {
        <div class="layout">
          <aside class="mapcol">
            <app-shop-map [query]="mapQuery()" />
          </aside>
          <form [formGroup]="form" (ngSubmit)="submit()" class="form">
          <div class="grid2">
            <div class="fg">
              <label class="label">Nom <span class="req">*</span></label>
              <input class="input" formControlName="name" placeholder="Pokekool Store" />
            </div>
            <div class="fg">
              <label class="label">Ville</label>
              <input class="input" formControlName="city" />
            </div>
          </div>

          <div class="fg">
            <label class="label">Description</label>
            <textarea class="input" rows="3" formControlName="description"></textarea>
          </div>

          <div class="grid2">
            <div class="fg">
              <label class="label">Adresse</label>
              <input class="input" formControlName="address" />
            </div>
            <div class="fg">
              <label class="label">Code postal</label>
              <input class="input" formControlName="zipCode" />
            </div>
          </div>

          <div class="grid2">
            <div class="fg">
              <label class="label">Lien Google Maps</label>
              <input class="input" formControlName="googleMapsUrl" placeholder="https://maps.google.com/..." />
            </div>
            <div class="fg">
              <label class="label">Instagram</label>
              <input class="input" formControlName="instagramUrl" placeholder="https://instagram.com/..." />
            </div>
          </div>

          <div class="grid2">
            <div class="fg">
              <label class="label">TikTok</label>
              <input class="input" formControlName="tiktokUrl" placeholder="https://tiktok.com/@..." />
            </div>
            <div class="fg">
              <label class="label">Photos (URLs, une par ligne)</label>
              <textarea class="input" rows="2" formControlName="photoUrlsText"
                        placeholder="https://.../photo1.jpg"></textarea>
            </div>
          </div>

          <fieldset class="fieldset">
            <legend>Services proposés</legend>
            <div class="chips">
              @for (s of allServices; track s.key) {
                <label class="chip" [class.chip--on]="isService(s.key)">
                  <input type="checkbox" [checked]="isService(s.key)" (change)="toggleService(s.key)" />
                  {{ s.label }}
                </label>
              }
            </div>
          </fieldset>

          <fieldset class="fieldset" formGroupName="hours">
            <legend>Horaires d'ouverture</legend>
            <div class="hours">
              @for (d of days; track d.key) {
                <div class="hrow" [formGroupName]="d.key">
                  <span class="hday">{{ d.label }}</span>
                  <label class="closed">
                    <input type="checkbox" formControlName="closed" /> Fermé
                  </label>
                  <input class="time" type="time" formControlName="open" [disabled]="isClosed(d.key)" />
                  <span class="sep">→</span>
                  <input class="time" type="time" formControlName="close" [disabled]="isClosed(d.key)" />
                </div>
              }
            </div>
          </fieldset>

          <!-- Prévisualisation -->
          <fieldset class="fieldset">
            <legend>Aperçu horaires</legend>
            <ul class="preview">
              @for (d of days; track d.key) {
                <li>
                  <strong>{{ d.label }}</strong>
                  @if (isClosed(d.key)) { <span class="muted">Fermé</span> }
                  @else { <span>{{ dayValue(d.key).open }} – {{ dayValue(d.key).close }}</span> }
                </li>
              }
            </ul>
          </fieldset>

          <div class="actions">
            <button type="submit" class="btn-primary" [disabled]="store.saving() || form.invalid">
              @if (store.saving()) { <app-spinner size="sm" /> } Enregistrer
            </button>
          </div>
        </form>
        </div>
      }
    </div>
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-6); }
    .layout { display: grid; grid-template-columns: 400px 1fr; gap: var(--space-6); align-items: start; }
    .mapcol { position: sticky; top: var(--space-4); }
    @media (max-width: 960px) { .layout { grid-template-columns: 1fr; } }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }
    .form { display: flex; flex-direction: column; gap: var(--space-5); }
    .grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-4); }
    .fg { display: flex; flex-direction: column; gap: var(--space-1); }
    .label { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-secondary); }
    .req { color: var(--color-semantic-danger); }
    .input {
      padding: var(--space-2) var(--space-3); border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); background: var(--color-surface-base);
      color: var(--color-text-primary); font-size: var(--font-size-sm); width: 100%;
    }
    .fieldset { border: 1px solid var(--color-border-subtle); border-radius: var(--radius-md); padding: var(--space-4); }
    legend { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); padding: 0 var(--space-2); }
    .chips { display: flex; flex-wrap: wrap; gap: var(--space-2); }
    .chip {
      display: inline-flex; align-items: center; gap: var(--space-1);
      padding: var(--space-1) var(--space-3); border-radius: var(--radius-full);
      border: 1px solid var(--color-border-default); font-size: var(--font-size-sm); cursor: pointer;
    }
    .chip--on { background: var(--color-brand-primary-subtle); color: var(--color-brand-primary); border-color: var(--color-brand-primary); }
    .chip input { display: none; }
    .hours { display: flex; flex-direction: column; gap: var(--space-2); }
    .hrow { display: grid; grid-template-columns: 100px 90px 1fr auto 1fr; align-items: center; gap: var(--space-2); }
    .hday { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); }
    .closed { font-size: var(--font-size-xs); color: var(--color-text-muted); display: inline-flex; gap: 4px; align-items: center; }
    .time { padding: var(--space-1) var(--space-2); border: 1px solid var(--color-border-default); border-radius: var(--radius-sm); }
    .sep { color: var(--color-text-muted); text-align: center; }
    .preview { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-1); }
    .preview li { display: flex; gap: var(--space-2); font-size: var(--font-size-sm); }
    .muted { color: var(--color-text-muted); }
    .actions { display: flex; justify-content: flex-end; }
    .btn-primary {
      display: inline-flex; align-items: center; gap: var(--space-2);
      height: 38px; padding: 0 var(--space-5); border: none; border-radius: var(--radius-md);
      background: var(--color-brand-primary); color: var(--color-brand-on-primary);
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); cursor: pointer;
    }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
  `],
})
export class ShopInfoComponent implements OnInit {
  readonly store = inject(ShopInfoStore);
  private readonly fb = inject(FormBuilder);
  readonly days = DAYS;
  readonly allServices = ALL_SERVICES;

  private readonly selectedServices = signal<string[]>([]);
  readonly mapQuery = signal('');

  readonly form: FormGroup = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    address: [''],
    city: [''],
    zipCode: [''],
    googleMapsUrl: [''],
    instagramUrl: [''],
    tiktokUrl: [''],
    photoUrlsText: [''],
    hours: this.fb.group(
      DAYS.reduce((acc, d) => {
        acc[d.key] = this.fb.group({ open: ['09:00'], close: ['19:00'], closed: [false] });
        return acc;
      }, {} as Record<string, FormGroup>),
    ),
  });

  ngOnInit(): void {
    this.store.load();
    this.form.valueChanges.subscribe(() => this.updateMapQuery());
    // Hydrate le formulaire dès que la boutique est chargée.
    queueMicrotask(() => this.hydrateWhenReady());
  }

  private hydrateWhenReady(): void {
    const check = () => {
      const shop = this.store.shop();
      if (!shop) { setTimeout(check, 80); return; }
      this.form.patchValue({
        name: shop.name ?? '',
        description: shop.description ?? '',
        address: shop.address ?? '',
        city: shop.city ?? '',
        zipCode: shop.zipCode ?? '',
        googleMapsUrl: shop.googleMapsUrl ?? '',
        instagramUrl: shop.instagramUrl ?? '',
        tiktokUrl: shop.tiktokUrl ?? '',
        photoUrlsText: (shop.photoUrls ?? []).join('\n'),
      });
      this.selectedServices.set([...(shop.services ?? [])]);
      this.updateMapQuery();
      if (shop.hoursJson) {
        try {
          const parsed = JSON.parse(shop.hoursJson) as WeekHours;
          const hg = this.form.get('hours') as FormGroup;
          for (const d of DAYS) {
            if (parsed[d.key]) hg.get(d.key)?.patchValue(parsed[d.key]);
          }
        } catch { /* JSON invalide : on garde les valeurs par défaut */ }
      }
    };
    check();
  }

  private updateMapQuery(): void {
    const v = this.form.getRawValue();
    const loc = [v.zipCode, v.city].map((x: string) => (x || '').trim()).filter(Boolean).join(' ');
    const parts = [v.name, v.address, loc].map((x: string) => (x || '').trim()).filter(Boolean);
    this.mapQuery.set(parts.join(', '));
  }

  isService(key: string): boolean { return this.selectedServices().includes(key); }
  toggleService(key: string): void {
    const cur = this.selectedServices();
    this.selectedServices.set(cur.includes(key) ? cur.filter(s => s !== key) : [...cur, key]);
  }

  isClosed(dayKey: string): boolean {
    return !!this.form.get(['hours', dayKey, 'closed'])?.value;
  }
  dayValue(dayKey: string): DayHours {
    return this.form.get(['hours', dayKey])?.value as DayHours;
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.getRawValue();
    const photoUrls = (v.photoUrlsText as string)
      .split('\n').map(s => s.trim()).filter(Boolean);

    const body: UpdateShopInfoRequest = {
      name: v.name,
      description: v.description || null,
      address: v.address || null,
      city: v.city || null,
      zipCode: v.zipCode || null,
      googleMapsUrl: v.googleMapsUrl || null,
      instagramUrl: v.instagramUrl || null,
      tiktokUrl: v.tiktokUrl || null,
      photoUrls,
      services: this.selectedServices(),
      hoursJson: JSON.stringify(v.hours),
    };
    this.store.save({ body });
  }
}
