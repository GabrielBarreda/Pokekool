import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  OnDestroy,
  HostListener,
  inject,
} from '@angular/core';
import { CollectionStore } from '../collection.store';
import { CollectionCardComponent } from '../collection-card/collection-card.component';
import { CollectionFiltersComponent } from '../collection-filters/collection-filters.component';
import { CollectionCardDto, SetOption } from '../collection.models';
// CollectionFilters used via store.updateFilters(partial)
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';

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
      </div>
    }
  `,
})
export class CollectionPageComponent implements OnInit, OnDestroy {
  readonly store = inject(CollectionStore);
  private readonly http = inject(HttpClient);

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
}
