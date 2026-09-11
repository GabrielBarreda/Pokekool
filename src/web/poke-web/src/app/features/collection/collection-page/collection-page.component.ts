import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  OnDestroy,
  HostListener,
  ViewChild,
  inject,
  signal,
  computed,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CollectionStore } from '../collection.store';
import { CollectionCardComponent } from '../collection-card/collection-card.component';
import { CollectionFiltersComponent } from '../collection-filters/collection-filters.component';
import { CollectionCardDto, SetOption } from '../collection.models';
import { CARD_CONDITIONS, conditionLabel } from '@shared/constants/card-condition.labels';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { ToastService } from '@shared/services/toast.service';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';
import { ThemeStore } from '@core/theme/theme.store';
import { PokemonBinderComponent, BinderItem } from '@shared/ui/pokemon-binder/pokemon-binder.component';

@Component({
  selector: 'app-collection-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CollectionCardComponent,
    CollectionFiltersComponent,
    PageHeaderComponent,
    SpinnerComponent,
    EmptyStateComponent,
    FormsModule,
    PokemonBinderComponent,
  ],
  styles: [`
    :host { display: block; }

    .page { padding: var(--space-4) var(--space-6); max-width: 1600px; margin: 0 auto; }

    .card-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(130px, 1fr));
      gap: var(--space-3);
      margin: var(--space-4) 0;
    }

    @media (min-width: 1200px) {
      .card-grid { grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); }
    }

    .pagination {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
      padding: var(--space-4) 0;
    }

    .pagination button {
      padding: var(--space-1) var(--space-3);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      background: var(--color-surface-raised);
      color: var(--color-text-primary);
      cursor: pointer;
      font-size: var(--font-size-sm);
      min-width: 36px;
    }

    .pagination button:disabled { opacity: 0.4; cursor: not-allowed; }
    .pagination button.active {
      background: var(--color-brand-primary);
      color: white;
      border-color: var(--color-brand-primary);
    }

    .pagination-info {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      margin: 0 var(--space-2);
    }

    /* Context menu */
    .context-menu {
      position: fixed;
      z-index: 1000;
      min-width: 200px;
      background: var(--color-surface-raised);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      box-shadow: var(--shadow-lg);
      overflow: hidden;
    }

    .context-menu__item {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      font-size: var(--font-size-sm);
      color: var(--color-text-primary);
      cursor: pointer;
      border: none;
      background: none;
      width: 100%;
      text-align: left;
      transition: background var(--duration-fast);
    }

    .context-menu__item:hover { background: var(--color-neutral-100); }
    .context-menu__item.disabled { color: var(--color-text-muted); cursor: default; }
    .context-menu__item.disabled:hover { background: none; }

    .context-menu__divider {
      height: 1px;
      background: var(--color-border-subtle);
      margin: var(--space-1) 0;
    }

    .loading-overlay {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 300px;
    }

    .stats-label {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
    }

    /* Modale inline (stock boutique) */
    .modal-backdrop {
      position: fixed; inset: 0; z-index: 2000;
      background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
    }
    .modal-box {
      background: var(--color-surface-raised);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-xl);
      padding: var(--space-5);
      min-width: 320px;
      max-width: 400px;
      width: 100%;
      display: flex; flex-direction: column; gap: var(--space-4);
    }
    .modal-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; }
    .modal-sub   { font-size: var(--font-size-sm); color: var(--color-text-muted); margin: 0; }
    .modal-field { display: flex; flex-direction: column; gap: var(--space-1); }
    .modal-field label { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); }
    .modal-field select,
    .modal-field input  { height: 36px; padding: 0 var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); color: var(--color-text-primary); font-size: var(--font-size-sm); }
    .modal-actions { display: flex; justify-content: flex-end; gap: var(--space-2); }
    .modal-actions button { padding: var(--space-2) var(--space-4); border-radius: var(--radius-md); font-size: var(--font-size-sm); cursor: pointer; border: 1px solid var(--color-border-default); background: var(--color-surface-base); }
    .modal-actions button.primary { background: var(--color-brand-primary); color: #fff; border-color: var(--color-brand-primary); }
    .modal-actions button:disabled { opacity: 0.5; cursor: not-allowed; }

  `],
  template: `
    <div class="page" (click)="onPageClick()">
      <app-page-header
        title="Ma Collection"
        [subtitle]="store.statsLabel()"
      />

      <app-collection-filters
        [filters]="store.filters()"
        [sets]="availableSets"
        [rarities]="store.availableRarities()"
        [years]="store.availableYears()"
        (filtersChange)="store.updateFilters($event)"
      />

      @if (store.loading() && store.cards().length === 0) {
        <div class="loading-overlay">
          <app-spinner />
        </div>
      } @else if (store.hasCards()) {
        @if (themeStore.isPokemon()) {
          <app-pokemon-binder
            #binder
            [items]="binderItems()"
            (itemClick)="onBinderItemClick($event)"
            (itemContextMenu)="onBinderContextMenu($event)"
          />
        } @else {
          <div class="card-grid">
            @for (card of store.cards(); track card.id) {
              <app-collection-card
                [card]="card"
                [isToggling]="store.toggling().has(card.id)"
                (cardClick)="store.toggleOwned(card.id)"
                (contextMenu)="onContextMenu($event, card)"
              />
            }
          </div>
        }

        <!-- Pagination -->
        @if (store.totalPages() > 1) {
          <div class="pagination">
            <button
              [disabled]="store.filters().page <= 1"
              (click)="goToPage(store.filters().page - 1); $event.stopPropagation()"
            >&lt;</button>

            @for (p of pageNumbers(); track p) {
              @if (p === -1) {
                <span class="pagination-info">…</span>
              } @else {
                <button
                  [class.active]="p === store.filters().page"
                  (click)="goToPage(p); $event.stopPropagation()"
                >{{ p }}</button>
              }
            }

            <button
              [disabled]="store.filters().page >= store.totalPages()"
              (click)="goToPage(store.filters().page + 1); $event.stopPropagation()"
            >&gt;</button>

            <span class="pagination-info">
              {{ store.totalItems() }} cartes
            </span>
          </div>
        }
      } @else {
        <app-empty-state
          icon="🃏"
          title="Aucune carte trouvée"
          description="Modifiez les filtres pour afficher des cartes."
        />
      }
    </div>

    <!-- Context menu -->
    @if (store.contextMenu(); as ctx) {
      <div
        class="context-menu"
        [style.left.px]="ctx.x"
        [style.top.px]="ctx.y"
        (click)="$event.stopPropagation()"
      >
        @if (themeStore.isPokemon()) {
          <button class="context-menu__item" (click)="onVisualizeAction(ctx.card)">
            👁 Visualiser
          </button>
          <div class="context-menu__divider"></div>
        }

        <button class="context-menu__item" (click)="onFavoriteAction(ctx.card)">
          {{ ctx.card.isFavorite ? '★ Retirer des favoris' : '☆ Ajouter aux favoris' }}
        </button>

        @if (!ctx.card.isOwned) {
          <button class="context-menu__item" (click)="onPriorityAction(ctx.card)">
            {{ ctx.card.isPriority ? '⚑ Retirer prioritaire' : '⚑ Marquer prioritaire' }}
          </button>
        }

        <div class="context-menu__divider"></div>

        <button class="context-menu__item disabled">
          + Créer un deck · Bientôt disponible
        </button>

        @if (ctx.card.isOwned) {
          <div class="context-menu__divider"></div>
          <button class="context-menu__item" (click)="openStockModal(ctx.card)">
            📦 Ajouter au stock boutique
          </button>
        }
      </div>
    }

    <!-- Modale : ajouter au stock boutique -->
    @if (stockModal(); as card) {
      <div class="modal-backdrop" (click)="stockModal.set(null)">
        <div class="modal-box" (click)="$event.stopPropagation()">
          <p class="modal-title">Ajouter au stock boutique</p>
          <p class="modal-sub">{{ card.displayName || card.name }} · {{ card.setName }}</p>

          <div class="modal-field">
            <label>État</label>
            <select [(ngModel)]="stockCondition">
              @for (c of conditions; track c.code) { <option [value]="c.code">{{ c.label }}</option> }
            </select>
          </div>

          <div class="modal-field">
            <label>Quantité</label>
            <input type="number" min="1" [(ngModel)]="stockQty" />
          </div>

          <div class="modal-field">
            <label>Prix d'achat (optionnel)</label>
            <input type="number" min="0" step="0.01" placeholder="—" [(ngModel)]="stockPrice" />
          </div>

          <div class="modal-actions">
            <button (click)="stockModal.set(null)">Annuler</button>
            <button class="primary" [disabled]="stockSaving()" (click)="confirmAddToStock(card)">
              {{ stockSaving() ? '…' : 'Ajouter' }}
            </button>
          </div>
        </div>
      </div>
    }

  `,
})
export class CollectionPageComponent implements OnInit, OnDestroy {
  readonly store = inject(CollectionStore);
  readonly themeStore = inject(ThemeStore);
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  // ── Thème Pokémon — classeur ───────────────────────────────────
  @ViewChild('binder') binderRef?: PokemonBinderComponent;

  readonly binderItems = computed<BinderItem[]>(() =>
    this.store.cards().map(card => ({
      id: card.id,
      imageUrl: card.imageSmall,
      imageLargeUrl: card.imageLarge ?? card.imageSmall,
      name: card.name,
      displayName: card.displayName,
      owned: card.isOwned,
      favorite: card.isFavorite,
      priority: card.isPriority && !card.isOwned,
    }))
  );

  // ── Stock boutique modal ──────────────────────────────────────
  readonly stockModal  = signal<CollectionCardDto | null>(null);
  readonly stockSaving = signal(false);
  stockCondition = 'NM';
  readonly conditions = CARD_CONDITIONS;
  readonly conditionLabel = conditionLabel;
  stockQty       = 1;
  stockPrice: number | null = null;

  availableSets: SetOption[] = [];

  ngOnInit(): void {
    this.store.loadCards();
    this.store.loadStats();

    // Load sets for filter from existing pokemon endpoint
    this.http.get<Array<{ id: string; name: string; releaseDate: string | null }>>('/api/v1/pokemon/sets').pipe(
      map(sets => sets.map(s => ({
        id: s.id,
        name: s.name,
        year: s.releaseDate ? new Date(s.releaseDate).getFullYear() : null,
      })))
    ).subscribe(sets => {
      this.availableSets = sets.sort((a, b) => a.name.localeCompare(b.name));
    });
  }

  ngOnDestroy(): void {
    this.store.closeContextMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.store.closeContextMenu();
  }

  onPageClick(): void {
    this.store.closeContextMenu();
  }

  onContextMenu(event: MouseEvent, card: CollectionCardDto): void {
    event.preventDefault();
    event.stopPropagation();

    // Clamp to viewport edges
    const menuWidth = 210;
    const menuHeight = 130;
    const x = Math.min(event.clientX, window.innerWidth - menuWidth);
    const y = Math.min(event.clientY, window.innerHeight - menuHeight);

    this.store.openContextMenu({ card, x, y });
  }

  // ── Thème Pokémon — classeur ───────────────────────────────────
  onBinderItemClick(item: BinderItem): void {
    this.store.toggleOwned(item.id);
  }

  onBinderContextMenu({ item, event }: { item: BinderItem; event: MouseEvent }): void {
    const card = this.store.cards().find(c => c.id === item.id);
    if (!card) return;
    this.onContextMenu(event, card);
  }

  onVisualizeAction(card: CollectionCardDto): void {
    this.store.closeContextMenu();
    this.binderRef?.viewItem(card.id);
  }

  onFavoriteAction(card: CollectionCardDto): void {
    this.store.setFavorite(card.id, !card.isFavorite);
  }

  onPriorityAction(card: CollectionCardDto): void {
    this.store.setPriority(card.id, !card.isPriority);
  }

  goToPage(page: number): void {
    this.store.setPage(page);
  }

  pageNumbers(): number[] {
    const current = this.store.filters().page;
    const total = this.store.totalPages();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);

    const pages: number[] = [1];
    if (current > 3) pages.push(-1);
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      pages.push(i);
    }
    if (current < total - 2) pages.push(-1);
    pages.push(total);
    return pages;
  }

  // ── Stock boutique ────────────────────────────────────────────
  openStockModal(card: CollectionCardDto): void {
    this.stockCondition = 'NM';
    this.stockQty       = 1;
    this.stockPrice     = null;
    this.store.closeContextMenu();
    this.stockModal.set(card);
  }

  confirmAddToStock(card: CollectionCardDto): void {
    if (this.stockSaving()) return;
    this.stockSaving.set(true);

    const body = {
      cardId:            card.id,
      productName:       card.name,
      type:              'CardRaw',
      condition:         this.stockCondition,
      gradingCompany:    null,
      gradeValue:        null,
      quantityTotal:     this.stockQty,
      quantityAvailable: this.stockQty,
      quantityReserved:  0,
      purchasePrice:     this.stockPrice ?? null,
      purchaseCurrency:  this.stockPrice != null ? 'EUR' : null,
      notes:             null,
    };

    this.http.post('/api/v1/admin/shop/stock', body).subscribe({
      next: () => {
        this.stockSaving.set(false);
        this.stockModal.set(null);
        this.toast.show({ message: `${card.displayName || card.name} ajouté au stock boutique.`, type: 'success' });
      },
      error: () => {
        this.stockSaving.set(false);
        this.toast.show({ message: 'Échec de l\'ajout au stock.', type: 'danger' });
      },
    });
  }
}
