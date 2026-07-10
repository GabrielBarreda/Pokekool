import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { ProductsStore } from '../../products.store';
import { BoutiqueService } from '../../boutique.service';
import {
  ShopProductDto, ShopProductRequest, ShopProductType,
  PRODUCT_TYPE_LABELS, CardSummary,
} from '../../boutique.models';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { ModalComponent } from '@shared/ui/modal/modal.component';
import { ConfirmDialogComponent } from '@shared/ui/confirm-dialog/confirm-dialog.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';
import { CARD_CONDITIONS, conditionLabel } from '@shared/constants/card-condition.labels';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ThemeStore } from '@core/theme/theme.store';

const EMPTY_FORM = (): ShopProductRequest => ({
  type: 'CardRaw', cardId: null, productName: null, condition: null,
  gradingCompany: null, gradeValue: null, price: 0, currency: 'EUR',
  quantity: 1, isAvailable: true, description: null, photoUrl: null,
});

@Component({
  selector: 'app-presentoir',
  standalone: true,
  imports: [
    CommonModule, FormsModule, PageHeaderComponent, SpinnerComponent, EmptyStateComponent,
    ModalComponent, ConfirmDialogComponent, BadgeComponent, HasPermissionDirective,
  ],
  template: `
    <div class="page">
      <app-page-header title="Présentoir" subtitle="Articles actuellement en vente">
        <button *hasPermission="'shop:write'" class="btn-primary" (click)="openCreate()">+ Nouvel article</button>
      </app-page-header>

      <!-- Filtres -->
      <div class="filters">
        <input class="input" placeholder="Rechercher…" [ngModel]="store.filters().search"
               (ngModelChange)="onSearch($event)" />
        <select class="input" [ngModel]="store.filters().type ?? ''" (ngModelChange)="onType($event)">
          <option value="">Tous les types</option>
          @for (t of typeKeys; track t) { <option [value]="t">{{ typeLabels[t] }}</option> }
        </select>
        <select class="input" [ngModel]="availabilityValue()" (ngModelChange)="onAvailability($event)">
          <option value="">Toutes dispos</option>
          <option value="true">Disponible</option>
          <option value="false">Indisponible</option>
        </select>
      </div>

      @if (store.loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else if (store.items().length === 0) {
        <app-empty-state icon="🃏" title="Aucun article"
          description="Ajoutez votre premier article au présentoir." />
      } @else {
        <div class="plv-board">
          <div class="plv-shelf">
            <div class="plv-shelf-label">
              À vendre · {{ store.items().length }} articles
            </div>

            <div class="plv-grid">
              @for (p of store.items(); track p.id) {
                <div class="plv-card-slot" [class.plv-card-slot--unavailable]="!p.isAvailable">

                  <!-- Image plein cadre (style CollectionCardComponent) -->
                  <div class="plv-card-img-wrap">
                    @if (p.photoUrl || p.cardImage) {
                      <img class="plv-card-img" [src]="p.photoUrl || p.cardImage" [alt]="p.cardName || p.productName" loading="lazy" />
                    } @else {
                      <div class="plv-card-placeholder">🃏</div>
                    }

                    <!-- Badge dispo (coin supérieur droit) -->
                    <span class="plv-dispo-badge" [class.plv-dispo-badge--off]="!p.isAvailable">
                      {{ p.isAvailable ? '●' : '○' }}
                    </span>

                    <!-- Overlay grisé si indisponible -->
                    @if (!p.isAvailable) {
                      <div class="plv-unavail-overlay">Épuisé</div>
                    }
                  </div>

                  <!-- Étiquette prix (style price tag carton) -->
                  <div class="plv-price-tag">
                    <div class="plv-price-tag__name">{{ p.cardName || p.productName }}</div>
                    <div class="plv-price-tag__sub">
                      {{ p.gradingCompany ? p.gradingCompany + ' ' + (p.gradeValue ?? '') : (conditionLabel(p.condition) || typeLabels[p.type]) }}
                      @if (p.quantity > 1) { · ×{{ p.quantity }} }
                    </div>
                    <div class="plv-price-tag__price">{{ p.price | number:'1.2-2' }} {{ p.currency }}</div>
                  </div>

                  <!-- Actions au hover -->
                  <div class="plv-card-actions">
                    <label class="plv-toggle" [title]="p.isAvailable ? 'Désactiver' : 'Activer'">
                      <input type="checkbox" [checked]="p.isAvailable" [disabled]="p.quantity === 0" (change)="toggle(p)" />
                      <span>{{ p.isAvailable ? 'En vente' : 'Inactif' }}</span>
                    </label>
                    <button *hasPermission="'shop:write'" class="plv-ic" (click)="openEdit(p)" title="Modifier">✎</button>
                    <button *hasPermission="'shop:write'" class="plv-ic plv-ic--danger" (click)="deleteTarget.set(p)" title="Supprimer">🗑</button>
                  </div>

                </div>
              }
            </div>
          </div>
        </div>

        @if (store.totalItems() > store.filters().pageSize) {
          <div class="plv-pagination-bar">
            <button class="plv-page-btn" [disabled]="store.filters().page <= 1"
                    (click)="store.setFilters({ page: store.filters().page - 1 })">← Précédent</button>
            <span class="plv-page-info">Page {{ store.filters().page }}</span>
            <button class="plv-page-btn" [disabled]="store.filters().page * store.filters().pageSize >= store.totalItems()"
                    (click)="store.setFilters({ page: store.filters().page + 1 })">Suivant →</button>
          </div>
        }
      }
    </div>

    <!-- Modale création / édition -->
    @if (modalOpen()) {
      <app-modal [title]="editId() ? 'Modifier l’article' : 'Nouvel article'" width="640px" (closed)="closeModal()">
        <div class="form">
          <div class="grid2">
            <div class="fg">
              <label class="label">Type</label>
              <select class="input" [(ngModel)]="draft.type">
                @for (t of typeKeys; track t) { <option [value]="t">{{ typeLabels[t] }}</option> }
              </select>
            </div>
            <div class="fg">
              <label class="label">Prix</label>
              <div class="inline">
                <input class="input" type="number" min="0" step="0.01" [(ngModel)]="draft.price" />
                <input class="input cur" [(ngModel)]="draft.currency" />
              </div>
            </div>
          </div>

          <!-- Sélecteur de carte (autocomplete) OU saisie libre -->
          <div class="fg">
            <label class="label">Carte liée (recherche) ou nom libre</label>
            @if (draft.cardId) {
              <div class="picked">
                <span>🃏 {{ pickedCardName() }}</span>
                <button class="link" (click)="clearCard()">changer</button>
              </div>
            } @else {
              <input class="input" placeholder="Rechercher une carte…" [ngModel]="cardQuery()"
                     (ngModelChange)="onCardSearch($event)" />
              @if (cardResults().length) {
                <ul class="ac">
                  @for (c of cardResults(); track c.id) {
                    <li (click)="pickCard(c)">
                      @if (c.imageSmall) { <img [src]="c.imageSmall" alt="" /> }
                      {{ c.name }} <span class="muted">· {{ c.setName }}</span>
                    </li>
                  }
                </ul>
              }
              <input class="input" placeholder="…ou nom d'un produit scellé" [(ngModel)]="draft.productName" />
            }
          </div>

          <div class="grid2">
            <div class="fg">
              <label class="label">État (RAW)</label>
              <select class="input" [(ngModel)]="draft.condition">
                <option [ngValue]="null">—</option>
                @for (c of conditions; track c.code) { <option [value]="c.code">{{ c.label }}</option> }
              </select>
            </div>
            <div class="fg">
              <label class="label">Gradation</label>
              <div class="inline">
                <select class="input" [(ngModel)]="draft.gradingCompany">
                  <option [ngValue]="null">—</option>
                  <option value="PSA">PSA</option><option value="BGS">BGS</option><option value="CGC">CGC</option>
                </select>
                <input class="input" type="number" step="0.5" placeholder="Note" [(ngModel)]="draft.gradeValue" />
              </div>
            </div>
          </div>

          <div class="grid2">
            <div class="fg">
              <label class="label">Quantité</label>
              <input class="input" type="number" min="0" [(ngModel)]="draft.quantity" />
            </div>
            <div class="fg">
              <label class="label chk">
                <input type="checkbox" [(ngModel)]="draft.isAvailable" [disabled]="draft.quantity === 0" />
                Disponible à la vente
              </label>
            </div>
          </div>

          <div class="fg">
            <label class="label">Photo (URL)</label>
            <input class="input" [(ngModel)]="draft.photoUrl" />
          </div>
          <div class="fg">
            <label class="label">Description</label>
            <textarea class="input" rows="2" [(ngModel)]="draft.description"></textarea>
          </div>

          <div class="actions">
            <button class="btn-ghost" (click)="closeModal()">Annuler</button>
            <button class="btn-primary" [disabled]="store.saving() || !isValid()" (click)="save()">
              @if (store.saving()) { <app-spinner size="sm" /> } Enregistrer
            </button>
          </div>
        </div>
      </app-modal>
    }

    @if (deleteTarget()) {
      <app-confirm-dialog title="Supprimer l'article"
        [message]="'Supprimer « ' + (deleteTarget()!.cardName || deleteTarget()!.productName) + ' » ?'"
        confirmLabel="Supprimer" variant="danger"
        (confirmed)="doDelete()" (cancelled)="deleteTarget.set(null)" />
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-5); }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }
    .filters { display: flex; gap: var(--space-3); flex-wrap: wrap; }
    .filters .input { max-width: 220px; }
    .input {
      padding: var(--space-2) var(--space-3); border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); background: var(--color-surface-base);
      color: var(--color-text-primary); font-size: var(--font-size-sm); width: 100%;
    }
    .form { display: flex; flex-direction: column; gap: var(--space-4); }
    .grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-3); }
    .fg { display: flex; flex-direction: column; gap: var(--space-1); }
    .label { font-size: var(--font-size-sm); color: var(--color-text-secondary); font-weight: var(--font-weight-medium); }
    .chk { flex-direction: row; align-items: center; gap: var(--space-2); margin-top: var(--space-5); }
    .inline { display: flex; gap: var(--space-2); }
    .cur { max-width: 70px; }
    .picked { display: flex; align-items: center; justify-content: space-between; padding: var(--space-2) var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md); }
    .link { border: none; background: none; color: var(--color-brand-primary); cursor: pointer; font-size: var(--font-size-sm); }
    .ac { list-style: none; margin: 4px 0 0; padding: 0; border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); max-height: 200px; overflow-y: auto; }
    .ac li { display: flex; align-items: center; gap: var(--space-2); padding: var(--space-2) var(--space-3); cursor: pointer; }
    .ac li:hover { background: var(--color-surface-raised); }
    .ac img { width: 22px; height: 30px; object-fit: cover; border-radius: 2px; }
    .muted { color: var(--color-text-muted); }
    .actions { display: flex; justify-content: flex-end; gap: var(--space-2); margin-top: var(--space-2); }
    .btn-primary { display:inline-flex; align-items:center; gap:6px; height:36px; padding:0 var(--space-4); border:none;
      border-radius: var(--radius-md); background: var(--color-brand-primary); color: var(--color-brand-on-primary);
      font-size: var(--font-size-sm); cursor: pointer; }
    .btn-primary:disabled { opacity:.6; cursor:not-allowed; }
    .btn-ghost { height:36px; padding:0 var(--space-4); border:1px solid var(--color-border-default);
      border-radius: var(--radius-md); background: transparent; color: var(--color-text-secondary); cursor:pointer; font-size: var(--font-size-sm); }

    /* ════════════════════════════════════════════════════════════
       PLV CARTON — Présentoir façon vrai display physique de
       boutique (planches/étagère + cartes posées dessus, étiquette
       de prix accrochée). Ce style s'applique toujours, quel que
       soit le thème actif.
       ════════════════════════════════════════════════════════════ */
    .plv-board {
      background:
        repeating-linear-gradient(
          0deg,
          transparent,
          transparent 3px,
          rgba(255,203,5,0.03) 3px,
          rgba(255,203,5,0.03) 4px
        ),
        linear-gradient(160deg, #16234f 0%, #0a132e 60%, #060a1c 100%);
      border-radius: 16px;
      padding: 24px 20px;
      box-shadow:
        0 8px 32px rgba(0,0,0,0.5),
        inset 0 1px 0 rgba(255,203,5,0.15),
        inset 0 -1px 0 rgba(0,0,0,0.3);
      border: 2px solid rgba(255,203,5,0.25);
    }

    .plv-shelf {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .plv-shelf-label {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      background: linear-gradient(90deg, #ffcb05, #f0a500);
      color: #060a1c;
      font-weight: 700;
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      padding: 4px 14px;
      border-radius: 4px;
      align-self: flex-start;
      box-shadow: 0 2px 6px rgba(0,0,0,0.4);
    }

    .plv-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(148px, 1fr));
      gap: 20px;
    }

    .plv-card-slot {
      display: flex;
      flex-direction: column;
      gap: 0;
      border-radius: 10px;
      overflow: hidden;
      background: #0d1a3a;
      border: 2px solid rgba(255,203,5,0.3);
      box-shadow: 0 4px 16px rgba(0,0,0,0.4);
      transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
      cursor: default;
      position: relative;
    }

    .plv-card-slot:hover {
      transform: translateY(-4px) scale(1.01);
      box-shadow: 0 8px 28px rgba(0,0,0,0.55), 0 0 0 1px rgba(255,203,5,0.6);
      border-color: rgba(255,203,5,0.7);
    }

    .plv-card-slot--unavailable {
      opacity: 0.55;
      filter: saturate(0.4);
    }

    .plv-card-img-wrap {
      position: relative;
      aspect-ratio: 2.5 / 3.5;
      overflow: hidden;
      background: #07111f;
    }

    .plv-card-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
      transition: transform 0.2s ease;
    }

    .plv-card-slot:hover .plv-card-img { transform: scale(1.04); }

    .plv-card-placeholder {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 36px;
      background: linear-gradient(135deg, #0d1a3a, #07111f);
      color: rgba(255,203,5,0.25);
    }

    .plv-dispo-badge {
      position: absolute;
      top: 6px;
      right: 6px;
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #22c55e;
      box-shadow: 0 0 6px rgba(34,197,94,0.7);
      font-size: 0;
    }
    .plv-dispo-badge--off {
      background: #ef4444;
      box-shadow: 0 0 6px rgba(239,68,68,0.6);
    }

    .plv-unavail-overlay {
      position: absolute;
      inset: 0;
      background: rgba(0,0,0,0.55);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #ef4444;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      backdrop-filter: grayscale(0.8);
    }

    .plv-price-tag {
      background: #fffdf0;
      color: #1a1a1a;
      padding: 8px 10px 10px;
      position: relative;
      border-top: 3px solid #ffcb05;
    }
    .plv-price-tag::before {
      content: '';
      position: absolute;
      top: -3px;
      left: 50%;
      transform: translateX(-50%);
      width: 20px;
      height: 3px;
      background: #ffcb05;
    }
    .plv-price-tag__name {
      font-size: 11px;
      font-weight: 700;
      color: #1a1a1a;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      line-height: 1.3;
    }
    .plv-price-tag__sub {
      font-size: 9px;
      color: #666;
      margin-top: 2px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .plv-price-tag__price {
      font-size: 15px;
      font-weight: 800;
      color: #c0392b;
      margin-top: 4px;
      letter-spacing: -0.02em;
    }

    .plv-card-actions {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 6px 8px;
      background: rgba(6,10,28,0.95);
      border-top: 1px solid rgba(255,203,5,0.15);
      opacity: 0;
      transition: opacity 0.15s ease;
    }
    .plv-card-slot:hover .plv-card-actions { opacity: 1; }

    .plv-ic {
      border: none;
      background: rgba(255,255,255,0.08);
      color: rgba(255,255,255,0.75);
      cursor: pointer;
      font-size: 14px;
      padding: 3px 7px;
      border-radius: 4px;
      transition: background 0.15s, color 0.15s;
    }
    .plv-ic:hover { background: rgba(255,255,255,0.15); color: #fff; }
    .plv-ic--danger:hover { background: rgba(239,68,68,0.2); color: #ef4444; }

    .plv-toggle {
      display: flex;
      align-items: center;
      gap: 4px;
      cursor: pointer;
      flex: 1;
    }
    .plv-toggle input { display: none; }
    .plv-toggle span {
      font-size: 9px;
      font-weight: 700;
      text-transform: uppercase;
      color: rgba(255,255,255,0.5);
      letter-spacing: 0.05em;
      padding: 2px 6px;
      border-radius: 3px;
      border: 1px solid rgba(255,255,255,0.15);
      transition: all 0.15s;
    }
    .plv-toggle input:checked + span {
      color: #22c55e;
      border-color: rgba(34,197,94,0.4);
      background: rgba(34,197,94,0.08);
    }

    .plv-pagination-bar {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-4);
      margin-top: var(--space-4);
    }
    .plv-page-btn {
      padding: var(--space-2) var(--space-4);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      font-size: var(--font-size-sm);
      cursor: pointer;
    }
    .plv-page-btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .plv-page-info { font-size: var(--font-size-sm); color: var(--color-text-muted); }
  `],
})
export class PresentoirComponent implements OnInit {
  readonly themeStore = inject(ThemeStore);
  readonly conditions = CARD_CONDITIONS;
  readonly conditionLabel = conditionLabel;
  readonly store = inject(ProductsStore);
  private readonly service = inject(BoutiqueService);

  readonly typeLabels = PRODUCT_TYPE_LABELS;
  readonly typeKeys = Object.keys(PRODUCT_TYPE_LABELS) as ShopProductType[];

  readonly modalOpen = signal(false);
  readonly editId = signal<string | null>(null);
  readonly deleteTarget = signal<ShopProductDto | null>(null);

  draft: ShopProductRequest = EMPTY_FORM();
  readonly pickedCardName = signal<string>('');

  // Autocomplete cartes
  readonly cardQuery = signal('');
  readonly cardResults = signal<CardSummary[]>([]);
  private readonly cardSearch$ = new Subject<string>();

  private readonly search$ = new Subject<string>();

  ngOnInit(): void {
    this.store.load();
    this.search$.pipe(debounceTime(300)).subscribe(v => this.store.setFilters({ search: v || null, page: 1 }));
    this.cardSearch$.pipe(
      debounceTime(300),
      switchMap(q => this.service.searchCards(q)),
    ).subscribe(r => this.cardResults.set(r));
  }

  availabilityValue(): string {
    const v = this.store.filters().isAvailable;
    return v == null ? '' : String(v);
  }
  onSearch(v: string): void { this.search$.next(v); }
  onType(v: string): void { this.store.setFilters({ type: (v || null) as ShopProductType | null, page: 1 }); }
  onAvailability(v: string): void {
    this.store.setFilters({ isAvailable: v === '' ? null : v === 'true', page: 1 });
  }

  openCreate(): void { this.draft = EMPTY_FORM(); this.editId.set(null); this.pickedCardName.set(''); this.modalOpen.set(true); }
  openEdit(p: ShopProductDto): void {
    this.editId.set(p.id);
    this.draft = {
      type: p.type, cardId: p.cardId, productName: p.productName, condition: p.condition,
      gradingCompany: p.gradingCompany, gradeValue: p.gradeValue, price: p.price,
      currency: p.currency, quantity: p.quantity, isAvailable: p.isAvailable,
      description: p.description, photoUrl: p.photoUrl,
    };
    this.pickedCardName.set(p.cardName ?? '');
    this.cardResults.set([]);
    this.modalOpen.set(true);
  }
  closeModal(): void { this.modalOpen.set(false); this.cardResults.set([]); this.cardQuery.set(''); }

  onCardSearch(q: string): void { this.cardQuery.set(q); if (q.trim().length >= 2) this.cardSearch$.next(q.trim()); }
  pickCard(c: CardSummary): void {
    this.draft.cardId = c.id; this.draft.productName = null;
    this.pickedCardName.set(c.name); this.cardResults.set([]); this.cardQuery.set('');
  }
  clearCard(): void { this.draft.cardId = null; this.pickedCardName.set(''); }

  isValid(): boolean {
    return (!!this.draft.cardId || !!this.draft.productName?.trim()) && this.draft.price >= 0 && this.draft.quantity >= 0;
  }

  save(): void {
    if (!this.isValid()) return;
    const id = this.editId();
    if (id) this.store.update({ id, body: this.draft, onSuccess: () => this.closeModal() });
    else this.store.create({ body: this.draft, onSuccess: () => this.closeModal() });
  }

  toggle(p: ShopProductDto): void { this.store.toggle({ id: p.id, isAvailable: !p.isAvailable }); }
  doDelete(): void {
    const t = this.deleteTarget();
    if (t) this.store.remove({ id: t.id });
    this.deleteTarget.set(null);
  }
}
