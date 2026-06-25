import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CollectionFilters, SetOption } from '../collection.models';

@Component({
  selector: 'app-collection-filters',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  styles: [`
    :host { display: block; }

    .filters-bar {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
      align-items: center;
      padding: var(--space-3) 0;
    }

    .filter-search {
      flex: 1 1 200px;
      min-width: 160px;
    }

    .filter-search input,
    .filter-select select {
      width: 100%;
      padding: var(--space-2) var(--space-3);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md);
      background: var(--color-surface-raised);
      color: var(--color-text-primary);
      font-size: var(--font-size-sm);
    }

    .filter-select {
      min-width: 140px;
    }

    .filter-checkboxes {
      display: flex;
      gap: var(--space-3);
      align-items: center;
    }

    .filter-checkboxes label {
      display: flex;
      align-items: center;
      gap: var(--space-1);
      font-size: var(--font-size-sm);
      color: var(--color-text-secondary);
      cursor: pointer;
      white-space: nowrap;
    }
  `],
  template: `
    <div class="filters-bar">
      <div class="filter-search">
        <input
          type="text"
          placeholder="Rechercher (nom, n°)…"
          [value]="filters.search"
          (input)="onSearch($event)"
        />
      </div>

      <div class="filter-select">
        <select [value]="filters.setId ?? ''" (change)="onSetChange($event)">
          <option value="">Tous les sets</option>
          @for (set of sets; track set.id) {
            <option [value]="set.id">{{ set.name }}</option>
          }
        </select>
      </div>

      @if (rarities.length > 0) {
        <div class="filter-select">
          <select [value]="filters.rarity ?? ''" (change)="onRarityChange($event)">
            <option value="">Toutes les raretés</option>
            @for (r of rarities; track r) {
              <option [value]="r">{{ r }}</option>
            }
          </select>
        </div>
      }

      @if (years.length > 0) {
        <div class="filter-select">
          <select [value]="filters.year ?? ''" (change)="onYearChange($event)">
            <option value="">Toutes les années</option>
            @for (y of years; track y) {
              <option [value]="y">{{ y }}</option>
            }
          </select>
        </div>
      }

      <div class="filter-select">
        <select [value]="filters.sortBy" (change)="onSortChange($event)">
          <option value="name_asc">Nom A→Z</option>
          <option value="name_desc">Nom Z→A</option>
          <option value="number">Numéro</option>
          <option value="rarity">Rareté</option>
        </select>
      </div>

      <div class="filter-checkboxes">
        <label>
          <input type="checkbox" [checked]="filters.ownedOnly" (change)="onOwnedOnlyChange($event)" />
          Possédées
        </label>
        <label>
          <input type="checkbox" [checked]="filters.favoritesOnly" (change)="onFavoritesOnlyChange($event)" />
          Favoris
        </label>
      </div>
    </div>
  `,
})
export class CollectionFiltersComponent {
  @Input({ required: true }) filters!: CollectionFilters;
  @Input() sets: SetOption[] = [];
  @Input() rarities: string[] = [];
  @Input() years: number[] = [];
  @Output() filtersChange = new EventEmitter<Partial<CollectionFilters>>();

  onSearch(event: Event): void {
    this.filtersChange.emit({ search: (event.target as HTMLInputElement).value, page: 1 });
  }

  onSetChange(event: Event): void {
    const val = (event.target as HTMLSelectElement).value;
    this.filtersChange.emit({ setId: val || null, page: 1 });
  }

  onRarityChange(event: Event): void {
    const val = (event.target as HTMLSelectElement).value;
    this.filtersChange.emit({ rarity: val || null, page: 1 });
  }

  onYearChange(event: Event): void {
    const val = (event.target as HTMLSelectElement).value;
    this.filtersChange.emit({ year: val ? +val : null, page: 1 });
  }

  onSortChange(event: Event): void {
    const val = (event.target as HTMLSelectElement).value as CollectionFilters['sortBy'];
    this.filtersChange.emit({ sortBy: val, page: 1 });
  }

  onOwnedOnlyChange(event: Event): void {
    this.filtersChange.emit({ ownedOnly: (event.target as HTMLInputElement).checked, page: 1 });
  }

  onFavoritesOnlyChange(event: Event): void {
    this.filtersChange.emit({ favoritesOnly: (event.target as HTMLInputElement).checked, page: 1 });
  }
}
