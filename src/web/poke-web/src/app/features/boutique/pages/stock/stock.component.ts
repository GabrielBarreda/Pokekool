import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject, catchError, debounceTime, forkJoin, map, of, switchMap } from 'rxjs';
import { StockStore } from '../../stock.store';
import { BoutiqueService } from '../../boutique.service';
import {
  ShopStockDto, ShopStockRequest, ShopProductType, PRODUCT_TYPE_LABELS, CardSummary,
  CARD_LANGUAGES, GRADING_COMPANIES, CardLanguage,
} from '../../boutique.models';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { ModalComponent } from '@shared/ui/modal/modal.component';
import { ConfirmDialogComponent } from '@shared/ui/confirm-dialog/confirm-dialog.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import { CARD_CONDITIONS, conditionLabel } from '@shared/constants/card-condition.labels';

const EMPTY = (): ShopStockRequest => ({
  cardId: null, productName: null, type: 'CardRaw', language: 'EN',
  condition: null, gradingCompany: null, gradeValue: null,
  quantityTotal: 1, quantityAvailable: 0, quantityReserved: 0,
  purchasePrice: null, purchaseCurrency: 'EUR', notes: null,
});

@Component({
  selector: 'app-stock',
  standalone: true,
  imports: [
    CommonModule, FormsModule, PageHeaderComponent, SpinnerComponent, EmptyStateComponent,
    ModalComponent, ConfirmDialogComponent, HasPermissionDirective,
  ],
  template: `
    <div class="page">
      <app-page-header title="Stock boutique" subtitle="Tout ce que possède l’entité morale (interne)">
        <button *hasPermission="'shop:write'" class="btn-primary" (click)="openCreate()">+ Ajouter au stock</button>
      </app-page-header>

      <div class="filters">
        <input class="input" placeholder="Rechercher…" [ngModel]="store.filters().search" (ngModelChange)="onSearch($event)" />
        <select class="input" [ngModel]="store.filters().type ?? ''" (ngModelChange)="onType($event)">
          <option value="">Tous les types</option>
          @for (t of typeKeys; track t) { <option [value]="t">{{ typeLabels[t] }}</option> }
        </select>
      </div>

      @if (store.loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else if (store.items().length === 0) {
        <app-empty-state icon="📦" title="Stock vide" description="Ajoutez une première entrée de stock." />
      } @else {
        @if (selectAll()) {
          <div class="bulk-actions">
            <span class="bulk-label">Action groupée sur les {{ store.items().length }} ligne(s) affichée(s) :</span>
            <button *hasPermission="'shop:write'" class="ic ic--display" (click)="openBulkPresentoirModal()" title="Tout mettre en présentoir">🏪 Présentoir</button>
            <button *hasPermission="'shop:write'" class="ic ic--danger" (click)="bulkDeleteConfirm.set(true)" title="Tout retirer">🗑 Retirer</button>
          </div>
        }
        <table class="tbl">
          <thead>
            <tr>
              <th>Article</th><th>Type</th><th>Grade/État</th><th>Total</th><th>Dispo</th><th>Réservé</th><th>Prix achat</th>
              <th class="cell-actions-header">
                <label class="select-all">
                  <input type="checkbox" [ngModel]="selectAll()" (ngModelChange)="selectAll.set($event)" />
                  Tout sélectionner
                </label>
              </th>
            </tr>
          </thead>
          <tbody>
            @for (s of store.items(); track s.id) {
              <tr>
                <td class="cell-name">
                  @if (s.cardImage) { <img class="thumb" [src]="s.cardImage" alt="" /> }
                  <span>{{ s.displayName || s.cardName || s.productName }}</span>
                </td>
                <td>{{ typeLabels[s.type] }}</td>
                <td>{{ s.gradingCompany ? s.gradingCompany + ' ' + (s.gradeValue ?? '') : (conditionLabel(s.condition) || '—') }}</td>
                <td>{{ s.quantityTotal }}</td>
                <td>{{ s.quantityAvailable }}</td>
                <td>{{ s.quantityReserved }}</td>
                <td class="internal">{{ s.purchasePrice != null ? (s.purchasePrice | number:'1.2-2') + ' ' + (s.purchaseCurrency || '') : '—' }}</td>
                <td class="cell-actions">
                  @if (!selectAll()) {
                    <button *hasPermission="'shop:write'" class="ic ic--display" (click)="openPresentoirModal(s)" title="Mettre en présentoir">🏪</button>
                    <button *hasPermission="'shop:write'" class="ic" (click)="openEdit(s)" title="Modifier">✎</button>
                    <button *hasPermission="'shop:write'" class="ic ic--danger" (click)="deleteTarget.set(s)" title="Retirer">🗑</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>

    @if (modalOpen()) {
      <app-modal [title]="editId() ? 'Modifier le stock' : 'Ajouter au stock'" width="620px" (closed)="closeModal()">
        <div class="form">
          <div class="fg">
            <label class="label">Carte liée ou nom libre</label>
            @if (draft.cardId) {
              <div class="picked"><span>🃏 {{ pickedCardName() }}</span><button class="link" (click)="clearCard()">changer</button></div>
            } @else {
              <input class="input" placeholder="Rechercher une carte…" [ngModel]="cardQuery()" (ngModelChange)="onCardSearch($event)" />
              @if (cardResults().length) {
                <ul class="ac">
                  @for (c of cardResults(); track c.id) {
                    <li (click)="pickCard(c)">@if (c.imageSmall) { <img [src]="c.imageSmall" alt="" /> } {{ c.displayName || c.name }} <span class="muted">· {{ c.setName }}</span></li>
                  }
                </ul>
              }
              <input class="input" placeholder="…ou nom d'un produit scellé" [(ngModel)]="draft.productName" />
            }
          </div>

          <!-- Type + Langue -->
          <div class="grid2">
            <div class="fg">
              <label class="label">Type</label>
              <select class="input" [(ngModel)]="draft.type" (ngModelChange)="onTypeChange($event)">
                @for (t of typeKeys; track t) { <option [value]="t">{{ typeLabels[t] }}</option> }
              </select>
            </div>
            <div class="fg">
              <label class="label">Langue de la carte</label>
              <select class="input" [(ngModel)]="draft.language">
                @for (l of cardLanguages; track l.code) {
                  <option [value]="l.code">{{ l.flag }} {{ l.label }}</option>
                }
              </select>
            </div>
          </div>

          <!-- État (Raw uniquement) ou Gradation (Gradée uniquement) — jamais les deux -->
          @if (draft.type === 'CardRaw') {
            <div class="fg">
              <label class="label">État</label>
              <select class="input" [(ngModel)]="draft.condition">
                <option [ngValue]="null">— Sélectionner —</option>
                @for (c of conditions; track c.code) { <option [value]="c.code">{{ c.label }}</option> }
              </select>
            </div>
          }
          @if (draft.type === 'CardGraded') {
            <div class="grid2">
              <div class="fg">
                <label class="label">Société de gradation</label>
                <select class="input" [(ngModel)]="draft.gradingCompany">
                  <option [ngValue]="null">— Sélectionner —</option>
                  @for (g of gradingCompanies; track g) { <option [value]="g">{{ g }}</option> }
                </select>
              </div>
              <div class="fg">
                <label class="label">Note</label>
                <input class="input" type="number" step="0.5" min="1" max="10" placeholder="ex. 9.5" [(ngModel)]="draft.gradeValue" />
              </div>
            </div>
          }

          <div class="grid3">
            <div class="fg"><label class="label">Total</label><input class="input" type="number" min="0" [(ngModel)]="draft.quantityTotal" /></div>
            <div class="fg"><label class="label">Disponible</label><input class="input" type="number" min="0" [(ngModel)]="draft.quantityAvailable" /></div>
            <div class="fg"><label class="label">Réservé</label><input class="input" type="number" min="0" [(ngModel)]="draft.quantityReserved" /></div>
          </div>

          <div class="grid2">
            <div class="fg">
              <label class="label">Prix d'achat (interne)</label>
              <div class="inline">
                <input class="input" type="number" min="0" step="0.01" [(ngModel)]="draft.purchasePrice" />
                <input class="input cur" [(ngModel)]="draft.purchaseCurrency" />
              </div>
            </div>
            <div class="fg">
              <label class="label">Notes</label>
              <input class="input" [(ngModel)]="draft.notes" />
            </div>
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
      <app-confirm-dialog title="Retirer du stock"
        [message]="'Retirer « ' + (deleteTarget()!.displayName || deleteTarget()!.cardName || deleteTarget()!.productName) + ' » du stock ?'"
        confirmLabel="Retirer" variant="danger"
        (confirmed)="doDelete()" (cancelled)="deleteTarget.set(null)" />
    }

    @if (bulkDeleteConfirm()) {
      <app-confirm-dialog title="Retirer toutes les lignes"
        [message]="'Retirer les ' + store.items().length + ' entrée(s) affichée(s) du stock ? Cette action est irréversible.'"
        confirmLabel="Tout retirer" variant="danger"
        (confirmed)="doBulkDelete()" (cancelled)="bulkDeleteConfirm.set(false)" />
    }

    <!-- Modale : mettre en présentoir (groupé) -->
    @if (bulkPresentoirModal()) {
      <div class="modal-backdrop" (click)="bulkPresentoirModal.set(false)">
        <div class="modal-box" (click)="$event.stopPropagation()">
          <p class="modal-title">Mettre en présentoir — {{ store.items().length }} ligne(s)</p>
          <p class="modal-sub">Prix et devise ci-dessous appliqués à toutes les lignes affichées. Chaque ligne garde sa propre quantité disponible.</p>

          <div class="fg">
            <label class="label">Prix de vente (requis)</label>
            <div class="inline">
              <input class="input" type="number" min="0" step="0.01" placeholder="0.00" [(ngModel)]="bulkPresentoirPrice" />
              <input class="input cur" [(ngModel)]="bulkPresentoirCurrency" />
            </div>
          </div>

          <div class="modal-actions">
            <button class="btn-ghost" (click)="bulkPresentoirModal.set(false)">Annuler</button>
            <button class="btn-primary" [disabled]="bulkPresentoirSaving() || !bulkPresentoirPrice" (click)="confirmBulkPresentoir()">
              {{ bulkPresentoirSaving() ? '…' : 'Mettre tout en présentoir' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Modale : mettre en présentoir -->
    @if (presentoirModal(); as s) {
      <div class="modal-backdrop" (click)="presentoirModal.set(null)">
        <div class="modal-box" (click)="$event.stopPropagation()">
          <p class="modal-title">Mettre en présentoir</p>
          <p class="modal-sub">{{ s.displayName || s.cardName || s.productName }} · {{ conditionLabel(s.condition) }}</p>

          <div class="fg">
            <label class="label">Prix de vente (requis)</label>
            <div class="inline">
              <input class="input" type="number" min="0" step="0.01" placeholder="0.00" [(ngModel)]="presentoirPrice" />
              <input class="input cur" [(ngModel)]="presentoirCurrency" />
            </div>
          </div>

          <div class="fg">
            <label class="label">Quantité à exposer</label>
            <input class="input" type="number" min="1" [(ngModel)]="presentoirQty" />
          </div>

          <div class="modal-actions">
            <button class="btn-ghost" (click)="presentoirModal.set(null)">Annuler</button>
            <button class="btn-primary" [disabled]="presentoirSaving() || !presentoirPrice" (click)="confirmPresentoir(s)">
              {{ presentoirSaving() ? '…' : 'Mettre en présentoir' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-5); }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }
    .filters { display: flex; gap: var(--space-3); }
    .filters .input { max-width: 220px; }
    .input { padding: var(--space-2) var(--space-3); border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); background: var(--color-surface-base); color: var(--color-text-primary);
      font-size: var(--font-size-sm); width: 100%; }
    .tbl { width: 100%; border-collapse: collapse; font-size: var(--font-size-sm); }
    .tbl th { text-align: left; padding: var(--space-2) var(--space-3); color: var(--color-text-muted);
      font-weight: var(--font-weight-medium); border-bottom: 1px solid var(--color-border-subtle); }
    .tbl td { padding: var(--space-2) var(--space-3); border-bottom: 1px solid var(--color-border-subtle); }
    .cell-name { display: flex; align-items: center; gap: var(--space-2); }
    .thumb { width: 28px; height: 39px; object-fit: cover; border-radius: 3px; }
    .internal { color: var(--color-text-muted); }
    .cell-actions { text-align: right; white-space: nowrap; }
    .cell-actions-header { text-align: right; white-space: nowrap; font-weight: var(--font-weight-medium); }
    .select-all { display: inline-flex; align-items: center; gap: var(--space-1); cursor: pointer; font-weight: var(--font-weight-medium); }
    .bulk-actions { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-2) var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-raised); }
    .bulk-label { font-size: var(--font-size-sm); color: var(--color-text-secondary); margin-right: auto; }
    .bulk-actions .ic { font-size: var(--font-size-sm); padding: var(--space-1) var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md); }
    .modal-hint { font-size: var(--font-size-sm); color: var(--color-text-muted); margin: 0; }
    .ic { border: none; background: transparent; cursor: pointer; font-size: 15px; padding: 4px 6px; border-radius: var(--radius-sm); }
    .ic:hover { background: var(--color-surface-raised); }
    .ic--danger:hover { color: var(--color-semantic-danger); }
    .form { display: flex; flex-direction: column; gap: var(--space-4); }
    .grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-3); }
    .grid3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: var(--space-3); }
    .fg { display: flex; flex-direction: column; gap: var(--space-1); }
    .label { font-size: var(--font-size-sm); color: var(--color-text-secondary); font-weight: var(--font-weight-medium); }
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
    .actions { display: flex; justify-content: flex-end; gap: var(--space-2); }
    .btn-primary { display:inline-flex; align-items:center; gap:6px; height:36px; padding:0 var(--space-4); border:none;
      border-radius: var(--radius-md); background: var(--color-brand-primary); color: var(--color-brand-on-primary);
      font-size: var(--font-size-sm); cursor: pointer; }
    .btn-primary:disabled { opacity:.6; cursor:not-allowed; }
    .btn-ghost { height:36px; padding:0 var(--space-4); border:1px solid var(--color-border-default);
      border-radius: var(--radius-md); background: transparent; color: var(--color-text-secondary); cursor:pointer; font-size: var(--font-size-sm); }
    .ic--display:hover { color: var(--color-brand-primary); }
    /* Modale présentoir */
    .modal-backdrop { position: fixed; inset: 0; z-index: 2000; background: rgba(0,0,0,0.45); display: flex; align-items: center; justify-content: center; }
    .modal-box { background: var(--color-surface-raised); border: 1px solid var(--color-border-default); border-radius: var(--radius-lg); box-shadow: var(--shadow-xl); padding: var(--space-5); min-width: 320px; max-width: 420px; width: 100%; display: flex; flex-direction: column; gap: var(--space-4); }
    .modal-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; }
    .modal-sub { font-size: var(--font-size-sm); color: var(--color-text-muted); margin: 0; }
    .modal-actions { display: flex; justify-content: flex-end; gap: var(--space-2); }
  `],
})
export class StockComponent implements OnInit {
  readonly store = inject(StockStore);
  private readonly service = inject(BoutiqueService);
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  readonly typeLabels = PRODUCT_TYPE_LABELS;
  readonly typeKeys = Object.keys(PRODUCT_TYPE_LABELS) as ShopProductType[];
  readonly conditions = CARD_CONDITIONS;
  readonly conditionLabel = conditionLabel;
  readonly cardLanguages = CARD_LANGUAGES;
  readonly gradingCompanies = GRADING_COMPANIES;

  readonly modalOpen = signal(false);
  readonly editId = signal<string | null>(null);
  readonly deleteTarget = signal<ShopStockDto | null>(null);

  // ── Sélection groupée ────────────────────────────────────────
  // Un seul toggle global (pas de sélection ligne par ligne) : coché, il masque les 3 boutons
  // d'action sur chaque ligne et fait apparaître une seule barre d'action au-dessus du tableau,
  // qui agit sur TOUTES les lignes actuellement affichées (store.items(), donc déjà filtrées par
  // la recherche/le type en cours). Voir doBulkDelete / confirmBulkPresentoir.
  readonly selectAll = signal(false);
  readonly bulkDeleteConfirm = signal(false);
  readonly bulkPresentoirModal  = signal(false);
  readonly bulkPresentoirSaving = signal(false);
  bulkPresentoirPrice: number | null = null;
  bulkPresentoirCurrency = 'EUR';

  // ── Présentoir modal ─────────────────────────────────────────
  readonly presentoirModal   = signal<ShopStockDto | null>(null);
  readonly presentoirSaving  = signal(false);
  presentoirPrice: number | null = null;
  presentoirCurrency = 'EUR';
  presentoirQty = 1;

  draft: ShopStockRequest = EMPTY();
  readonly pickedCardName = signal('');
  readonly cardQuery = signal('');
  readonly cardResults = signal<CardSummary[]>([]);
  private readonly cardSearch$ = new Subject<string>();
  private readonly search$ = new Subject<string>();

  ngOnInit(): void {
    this.store.load();
    this.search$.pipe(debounceTime(300)).subscribe(v => this.store.setFilters({ search: v || null, page: 1 }));
    this.cardSearch$.pipe(debounceTime(300), switchMap(q => this.service.searchCards(q))).subscribe(r => this.cardResults.set(r));
  }

  onSearch(v: string): void { this.search$.next(v); }
  onType(v: string): void { this.store.setFilters({ type: (v || null) as ShopProductType | null, page: 1 }); }

  /** Quand on change le type, on remet à zéro les champs de l'autre mode. */
  onTypeChange(type: ShopProductType): void {
    if (type === 'CardRaw') {
      this.draft.gradingCompany = null;
      this.draft.gradeValue = null;
    } else if (type === 'CardGraded') {
      this.draft.condition = null;
    }
  }

  openCreate(): void { this.draft = EMPTY(); this.editId.set(null); this.pickedCardName.set(''); this.modalOpen.set(true); }
  openEdit(s: ShopStockDto): void {
    this.editId.set(s.id);
    this.draft = {
      cardId: s.cardId, productName: s.productName, type: s.type, language: s.language ?? 'EN',
      condition: s.condition, gradingCompany: s.gradingCompany, gradeValue: s.gradeValue,
      quantityTotal: s.quantityTotal, quantityAvailable: s.quantityAvailable, quantityReserved: s.quantityReserved,
      purchasePrice: s.purchasePrice, purchaseCurrency: s.purchaseCurrency, notes: s.notes,
    };
    this.pickedCardName.set(s.displayName || s.cardName || '');
    this.modalOpen.set(true);
  }
  closeModal(): void { this.modalOpen.set(false); this.cardResults.set([]); this.cardQuery.set(''); }

  onCardSearch(q: string): void { this.cardQuery.set(q); if (q.trim().length >= 2) this.cardSearch$.next(q.trim()); }
  pickCard(c: CardSummary): void { this.draft.cardId = c.id; this.draft.productName = null; this.pickedCardName.set(c.displayName || c.name); this.cardResults.set([]); this.cardQuery.set(''); }
  clearCard(): void { this.draft.cardId = null; this.pickedCardName.set(''); }

  isValid(): boolean { return (!!this.draft.cardId || !!this.draft.productName?.trim()) && this.draft.quantityTotal >= 0; }

  save(): void {
    if (!this.isValid()) return;
    const id = this.editId();
    if (id) this.store.update({ id, body: this.draft, onSuccess: () => this.closeModal() });
    else this.store.create({ body: this.draft, onSuccess: () => this.closeModal() });
  }
  doDelete(): void { const t = this.deleteTarget(); if (t) this.store.remove({ id: t.id }); this.deleteTarget.set(null); }

  // ── Présentoir ───────────────────────────────────────────────
  openPresentoirModal(s: ShopStockDto): void {
    this.presentoirPrice    = null;
    this.presentoirCurrency = 'EUR';
    this.presentoirQty      = Math.max(1, s.quantityAvailable);
    this.presentoirModal.set(s);
  }

  confirmPresentoir(s: ShopStockDto): void {
    const price = this.presentoirPrice;
    if (!price || this.presentoirSaving()) return;
    this.presentoirSaving.set(true);

    const body = {
      type:           s.type,
      cardId:         s.cardId,
      productName:    s.cardName ?? s.productName,
      language:       s.language ?? 'EN',
      condition:      s.condition,
      gradingCompany: s.gradingCompany,
      gradeValue:     s.gradeValue,
      price,
      currency:       this.presentoirCurrency,
      quantity:       this.presentoirQty,
      isAvailable:    true,
      description:    null,
      photoUrl:       s.cardImage,
    };

    this.http.post('/api/v1/admin/shop/products', body).subscribe({
      next: () => {
        this.presentoirSaving.set(false);
        this.presentoirModal.set(null);
        this.toast.show({ message: `${s.displayName || s.cardName || s.productName} ajouté au présentoir.`, type: 'success' });
      },
      error: () => {
        this.presentoirSaving.set(false);
        this.toast.show({ message: 'Échec de la mise en présentoir.', type: 'danger' });
      },
    });
  }

  // ── Actions groupées ("Tout sélectionner") ─────────────────────
  doBulkDelete(): void {
    this.bulkDeleteConfirm.set(false);
    const ids = this.store.items().map(s => s.id);
    if (ids.length === 0) return;
    this.store.removeMany({ ids });
    this.selectAll.set(false);
  }

  openBulkPresentoirModal(): void {
    this.bulkPresentoirPrice = null;
    this.bulkPresentoirCurrency = 'EUR';
    this.bulkPresentoirModal.set(true);
  }

  /** Prix/devise communs saisis une fois, appliqués à toutes les lignes affichées — chaque ligne
   * garde son propre cardId/type/condition/quantité disponible. Tolérant aux échecs partiels
   * (une ligne en échec n'empêche pas les autres, voir toasts de résumé). */
  confirmBulkPresentoir(): void {
    const price = this.bulkPresentoirPrice;
    const items = this.store.items();
    if (!price || items.length === 0 || this.bulkPresentoirSaving()) return;
    this.bulkPresentoirSaving.set(true);

    const requests = items.map(s => this.http.post('/api/v1/admin/shop/products', {
      type:           s.type,
      cardId:         s.cardId,
      productName:    s.cardName ?? s.productName,
      language:       s.language ?? 'EN',
      condition:      s.condition,
      gradingCompany: s.gradingCompany,
      gradeValue:     s.gradeValue,
      price,
      currency:       this.bulkPresentoirCurrency,
      quantity:       Math.max(1, s.quantityAvailable),
      isAvailable:    true,
      description:    null,
      photoUrl:       s.cardImage,
    }).pipe(map(() => true), catchError(() => of(false))));

    forkJoin(requests).subscribe(results => {
      this.bulkPresentoirSaving.set(false);
      this.bulkPresentoirModal.set(false);
      this.selectAll.set(false);

      const okCount = results.filter(Boolean).length;
      const failCount = results.length - okCount;
      if (okCount > 0) this.toast.show({ message: `${okCount} ligne(s) mise(s) en présentoir.`, type: 'success' });
      if (failCount > 0) this.toast.show({ message: `${failCount} échec(s) lors de la mise en présentoir.`, type: 'danger' });
    });
  }
}
