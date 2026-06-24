import {
  Component, inject, OnInit, ChangeDetectionStrategy, signal, computed, effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MarketStore } from '../market.store';
import { MarketCardDto } from '../market.models';
import { CardDetailComponent } from '../card-detail/card-detail.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { PermissionStore } from '@core/auth/permission.store';

@Component({
  selector: 'app-market-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, CardDetailComponent, PageHeaderComponent, SpinnerComponent, EmptyStateComponent],
  template: `
    <div class="page">
      <!-- Header -->
      <app-page-header title="Bourse" subtitle="Cours du marché · Données de gradation PSA, BGS, CGC…" />

      <!-- Set selector -->
      <div class="set-bar">
        @if (store.loadingSets()) {
          <div class="set-bar__loading"><app-spinner size="sm" /> Chargement des sets…</div>
        } @else {
          <div class="set-selector-wrap">
            <svg class="set-icon" width="16" height="16" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" stroke-width="2">
              <rect x="3" y="3" width="18" height="18" rx="2"/>
              <path d="M3 9h18M9 21V9"/>
            </svg>
            <select class="set-select" [ngModel]="store.selectedSetId()" (ngModelChange)="onSetChange($event)">
              @for (group of store.seriesGroups(); track group.series) {
                <optgroup [label]="group.series">
                  @for (s of group.sets; track s.id) {
                    <option [value]="s.id">
                      {{ s.cardCount === 0 ? '○' : '●' }} {{ s.name }}
                      @if (s.totalCards) { · {{ s.cardCount }}/{{ s.totalCards }} }
                    </option>
                  }
                </optgroup>
              }
            </select>
            @if (store.selectedSet(); as set) {
              <div class="set-completion">
                <span class="set-badge" [class.set-badge--empty]="set.cardCount === 0"
                      [title]="'Total TCG : ' + set.totalCards + ' · Importées en base : ' + set.cardCount">
                  @if (set.cardCount === 0) {
                    <span class="set-badge__sub">non importé</span>
                  } @else if (showPercent() && completionPercent() !== null) {
                    <span class="set-badge__pct">{{ completionPercent() }}%</span>
                    <span class="set-badge__sub">complété</span>
                  } @else {
                    {{ set.cardCount | number }} / {{ set.totalCards }}
                    <span class="set-badge__sub">importées</span>
                  }
                </span>
                @if (set.cardCount > 0) {
                  <label class="pct-toggle" [title]="showPercent() ? 'Afficher le nombre' : 'Afficher en %'">
                    <input type="checkbox" [checked]="showPercent()" (change)="showPercent.set(!showPercent())" />
                    <span>%</span>
                  </label>
                }
              </div>
            }
          </div>
        }
      </div>

      <!-- ── Panneau admin sync (visible si cards:sync) ── -->
      @if (canSync()) {
        <div class="sync-panel">
          <div class="sync-panel__header">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 2v6h-6M3 12a9 9 0 0 1 15-6.7L21 8M3 22v-6h6M21 12a9 9 0 0 1-15 6.7L3 16"/>
            </svg>
            <span class="sync-panel__title">Synchronisation données</span>
            <span class="sync-panel__scope">
              @if (store.selectedSet(); as set) {
                {{ set.cardCount }} / {{ set.totalCards }} importées
                @if (completionPercent() !== null) { · {{ completionPercent() }}% }
                · {{ set.name }}
              }
            </span>
          </div>

          <div class="sync-panel__actions">
            <!-- Sync Catalogue TCG -->
            <button class="sync-btn sync-btn--catalog"
                    [disabled]="store.isSyncing()"
                    (click)="onSyncCatalog()">
              @if (store.syncingCatalog()) {
                <app-spinner size="sm" />
                Import en cours…
              } @else {
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/>
                  <path d="M9 18H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4"/>
                </svg>
                Catalogue · TCG
              }
            </button>

            <!-- Sync Prix PriceCharting -->
            <button class="sync-btn sync-btn--prices"
                    [disabled]="store.isSyncing() || !store.hasCards()"
                    (click)="onSyncPrices()">
              @if (store.syncingPrices()) {
                <app-spinner size="sm" />
                Sync en cours…
              } @else {
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
                </svg>
                Prix · PriceCharting
              }
            </button>

            <!-- Sync Population PSA -->
            <button class="sync-btn sync-btn--psa"
                    [disabled]="store.isSyncing() || !store.hasCards()"
                    (click)="onSyncPopulation()">
              @if (store.syncingPopulation()) {
                <app-spinner size="sm" />
                Sync en cours…
              } @else {
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/>
                </svg>
                Population · PSA
              }
            </button>
          </div>

          <!-- Résultat du dernier sync -->
          @if (store.lastSyncResult(); as result) {
            <div class="sync-result" [class.sync-result--error]="(result.cardsFailed ?? 0) > 0">
              <div class="sync-result__row">
                @if ((result.cardsFailed ?? 0) === 0) {
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                    <path d="M20 6 9 17l-5-5"/>
                  </svg>
                } @else {
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                    <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                  </svg>
                }
                <span>
                  @if (result.type === 'prices') {
                    {{ result.pricesUpserted }} prix mis à jour · {{ result.cardsFailed }} échec(s)
                  } @else {
                    {{ result.populationsUpserted }} populations mises à jour · {{ result.cardsFailed }} échec(s)
                  }
                </span>
                <span class="sync-result__time">{{ result.completedAt | date:'HH:mm:ss' }}</span>
                <button class="sync-result__dismiss" (click)="store.dismissSyncResult()">×</button>
              </div>
              @if (result.errors && result.errors.length > 0) {
                <details class="sync-errors">
                  <summary>{{ result.errors.length }} erreur(s) détaillées</summary>
                  <ul>
                    @for (e of result.errors; track e) { <li>{{ e }}</li> }
                  </ul>
                </details>
              }
            </div>
          }
        </div>
      }

      <!-- Filtres -->
      <div class="filters">
        <div class="search-wrap">
          <svg class="search-icon" width="15" height="15" viewBox="0 0 24 24" fill="none"
               stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
          </svg>
          <input class="search-input" type="text" placeholder="Nom, numéro…"
                 [ngModel]="store.filters().search"
                 (ngModelChange)="onSearch($event)" />
        </div>

        <select class="filter-select"
                [ngModel]="store.filters().supertype"
                (ngModelChange)="onFilter('supertype', $event)">
          <option value="">Tous types</option>
          @for (s of SUPERTYPES; track s.value) {
            <option [value]="s.value"
                    [disabled]="store.availableSupertypes().length > 0 && !store.availableSupertypes().includes(s.value)">
              {{ s.label }}
            </option>
          }
        </select>

        <select class="filter-select"
                [ngModel]="store.filters().rarity"
                (ngModelChange)="onFilter('rarity', $event)">
          <option value="">Toutes raretés</option>
          @for (r of RARITIES; track r) {
            <option [value]="r"
                    [disabled]="store.availableRarities().length > 0 && !store.availableRarities().includes(r)">
              {{ r }}
            </option>
          }
        </select>

        @if (hasActiveFilter()) {
          <button class="btn-reset" (click)="resetFilters()">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M18 6 6 18M6 6l12 12"/>
            </svg>
            Réinitialiser
          </button>
        }
      </div>

      <!-- Grille -->
      <div class="content-area">
        @if (store.loadingCards() && store.cards().length === 0) {
          <div class="center"><app-spinner size="lg" /></div>
        } @else if (!store.selectedSetId()) {
          <app-empty-state icon="📦" title="Sélectionnez un set"
            description="Choisissez un set dans le menu déroulant pour consulter les cours." />
        } @else if (!store.hasCards() && !store.loadingCards()) {
          <app-empty-state icon="🔍" title="Aucune carte"
            description="Modifiez vos filtres ou choisissez un autre set." />
        } @else {
          <!-- Grid -->
          <div class="card-grid" [class.card-grid--loading]="store.loadingCards()">
            @for (card of store.cards(); track card.id) {
              <button class="pokemon-card" [class.pokemon-card--selected]="isSelected(card)"
                      (click)="selectCard(card)">
                <!-- Image -->
                <div class="card-img-wrap">
                  @if (card.imageSmall) {
                    <img [src]="card.imageSmall" [alt]="card.name" class="card-img" loading="lazy" />
                  } @else {
                    <div class="card-img card-img--placeholder">
                      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.3">
                        <rect x="3" y="3" width="18" height="18" rx="2"/>
                        <circle cx="12" cy="12" r="3"/>
                      </svg>
                    </div>
                  }
                  @if (card.alertCount > 0) {
                    <span class="alert-dot">{{ card.alertCount }}</span>
                  }
                </div>

                <!-- Info -->
                <div class="card-info">
                  <div class="card-name">{{ card.name }}</div>
                  <div class="card-meta">
                    <span class="card-number">#{{ card.number }}</span>
                    @if (card.rarity) {
                      <span class="card-rarity">{{ card.rarity }}</span>
                    }
                  </div>

                  <!-- Prix -->
                  @if (card.priceMarket !== null) {
                    <div class="card-price">
                      <span class="price-value">{{ card.priceMarket | currency:(card.priceCurrency ?? 'USD'):'symbol':'1.2-2' }}</span>
                      <span class="price-source">{{ formatSource(card.priceSource) }}</span>
                    </div>
                  } @else {
                    <div class="card-price card-price--none">Prix indisponible</div>
                  }
                </div>
              </button>
            }
          </div>

          <!-- Pagination -->
          @if (store.totalPages() > 1) {
            <div class="pagination">
              <button class="page-btn" [disabled]="store.filters().page <= 1"
                      (click)="setPage(store.filters().page - 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="m15 18-6-6 6-6"/>
                </svg>
              </button>
              @for (p of visiblePages(); track p) {
                @if (p === -1) {
                  <span class="page-ellipsis">…</span>
                } @else {
                  <button class="page-btn" [class.page-btn--active]="p === store.filters().page"
                          (click)="setPage(p)">{{ p }}</button>
                }
              }
              <button class="page-btn" [disabled]="store.filters().page >= store.totalPages()"
                      (click)="setPage(store.filters().page + 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="m9 18 6-6-6-6"/>
                </svg>
              </button>
            </div>
          }
        }
      </div>
    </div>

    <!-- Card detail drawer -->
    @if (store.selectedCard()) {
      <app-card-detail
        [card]="store.selectedCard()!"
        [gradingData]="store.gradingData()"
        (closed)="store.selectCard(null)" />
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-5); }
    .center { display: flex; justify-content: center; padding: var(--space-12) 0; }

    /* ── Set bar ── */
    .set-bar { background: var(--color-surface-base); border: 1px solid var(--color-border-default); border-radius: var(--radius-lg); padding: var(--space-3) var(--space-4); }
    .set-bar__loading { display: flex; align-items: center; gap: var(--space-2); font-size: var(--font-size-sm); color: var(--color-text-muted); }
    .set-selector-wrap { display: flex; align-items: center; gap: var(--space-3); }
    .set-icon { color: var(--color-text-muted); flex-shrink: 0; }
    .set-select {
      flex: 1; min-width: 0; max-width: 420px;
      height: 38px; padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-raised); font-size: var(--font-size-sm);
      color: var(--color-text-primary); cursor: pointer;
      &:focus { outline: 2px solid var(--color-brand-primary); outline-offset: 1px; }
    }
    .set-completion { display: flex; align-items: center; gap: var(--space-2); }
    .set-badge {
      display: flex; align-items: baseline; gap: 4px;
      font-size: var(--font-size-xs); font-weight: var(--font-weight-medium);
      background: var(--color-surface-sunken); border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-full); padding: 2px 10px; color: var(--color-text-secondary);
      white-space: nowrap;
    }
    .set-badge--empty { border-style: dashed; color: var(--color-text-disabled); }
    .set-badge__pct { font-size: var(--font-size-sm); font-weight: var(--font-weight-bold); color: var(--color-brand-primary); }
    .set-badge__sub { font-size: 10px; opacity: 0.65; }
    .pct-toggle {
      display: flex; align-items: center; gap: 3px;
      font-size: var(--font-size-xs); color: var(--color-text-muted);
      cursor: pointer; user-select: none;
      input[type="checkbox"] { width: 13px; height: 13px; accent-color: var(--color-brand-primary); cursor: pointer; }
      &:hover { color: var(--color-brand-primary); }
    }

    /* ── Sync panel ── */
    .sync-panel {
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-left: 3px solid var(--color-brand-primary);
      border-radius: var(--radius-lg);
      padding: var(--space-3) var(--space-4);
      display: flex; flex-direction: column; gap: var(--space-3);
    }
    .sync-panel__header {
      display: flex; align-items: center; gap: var(--space-2);
      color: var(--color-text-secondary); font-size: var(--font-size-sm);
      svg { color: var(--color-brand-primary); flex-shrink: 0; }
    }
    .sync-panel__title { font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .sync-panel__scope { margin-left: auto; font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .sync-panel__actions { display: flex; gap: var(--space-3); flex-wrap: wrap; }

    .sync-btn {
      display: flex; align-items: center; gap: var(--space-2);
      height: 36px; padding: 0 var(--space-4);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium);
      cursor: pointer; border: 1px solid transparent;
      transition: opacity var(--duration-fast), background var(--duration-fast);
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
    .sync-btn--catalog {
      background: var(--color-surface-raised);
      border-color: var(--color-border-default);
      color: var(--color-text-secondary);
      &:not(:disabled):hover { border-color: var(--color-brand-primary); color: var(--color-brand-primary); }
    }
    .sync-btn--prices {
      background: var(--color-brand-primary); color: #fff;
      &:not(:disabled):hover { opacity: 0.88; }
    }
    .sync-btn--psa {
      background: var(--color-surface-raised);
      border-color: var(--color-border-default);
      color: var(--color-text-primary);
      &:not(:disabled):hover { border-color: var(--color-brand-primary); color: var(--color-brand-primary); }
    }

    .sync-result {
      border-radius: var(--radius-md);
      padding: var(--space-2) var(--space-3);
      background: var(--color-semantic-success-subtle);
      border: 1px solid var(--color-semantic-success);
      color: var(--color-semantic-success);
      font-size: var(--font-size-sm);
      &--error {
        background: var(--color-semantic-danger-subtle);
        border-color: var(--color-semantic-danger);
        color: var(--color-semantic-danger);
      }
    }
    .sync-result__row {
      display: flex; align-items: center; gap: var(--space-2);
      svg { flex-shrink: 0; }
    }
    .sync-result__time { margin-left: auto; font-size: var(--font-size-xs); opacity: 0.7; font-family: var(--font-family-mono); }
    .sync-result__dismiss {
      all: unset; cursor: pointer; font-size: 16px; line-height: 1;
      opacity: 0.6; padding: 0 2px;
      &:hover { opacity: 1; }
    }
    .sync-errors {
      margin-top: var(--space-2); font-size: var(--font-size-xs);
      summary { cursor: pointer; opacity: 0.8; }
      ul { margin: var(--space-1) 0 0 var(--space-4); display: flex; flex-direction: column; gap: 2px; opacity: 0.8; }
    }

    /* ── Filtres ── */
    .filters { display: flex; align-items: center; gap: var(--space-3); flex-wrap: wrap; }
    .search-wrap { position: relative; flex: 1; min-width: 200px; max-width: 340px; }
    .search-icon { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); color: var(--color-text-muted); pointer-events: none; }
    .search-input {
      width: 100%; height: 38px; padding: 0 var(--space-3) 0 32px;
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      font-size: var(--font-size-sm); background: var(--color-surface-base);
      color: var(--color-text-primary);
      &:focus { outline: 2px solid var(--color-brand-primary); outline-offset: 1px; }
    }
    .filter-select {
      height: 38px; padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      font-size: var(--font-size-sm); background: var(--color-surface-base);
      color: var(--color-text-primary); cursor: pointer;
      &:focus { outline: 2px solid var(--color-brand-primary); outline-offset: 1px; }
    }
    .btn-reset {
      display: flex; align-items: center; gap: var(--space-1);
      height: 38px; padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-base); color: var(--color-text-secondary);
      font-size: var(--font-size-sm); cursor: pointer;
      &:hover { background: var(--color-semantic-danger-subtle); color: var(--color-semantic-danger); border-color: var(--color-semantic-danger); }
    }

    /* ── Content area ── */
    .content-area { display: flex; flex-direction: column; gap: var(--space-6); }

    /* ── Card grid ── */
    .card-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
      gap: var(--space-4);
      transition: opacity var(--duration-fast);
      &--loading { opacity: 0.5; pointer-events: none; }
    }

    .pokemon-card {
      all: unset; cursor: pointer;
      display: flex; flex-direction: column;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      overflow: hidden;
      transition: box-shadow var(--duration-fast), border-color var(--duration-fast), transform var(--duration-fast);
      &:hover { box-shadow: var(--shadow-md); border-color: var(--color-brand-primary); transform: translateY(-2px); }
      &--selected { border-color: var(--color-brand-primary); box-shadow: 0 0 0 2px var(--color-brand-primary); }
    }

    .card-img-wrap { position: relative; background: var(--color-neutral-100); aspect-ratio: 63/88; overflow: hidden; }
    .card-img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .card-img--placeholder { display: flex; align-items: center; justify-content: center; width: 100%; height: 100%; }
    .alert-dot {
      position: absolute; top: 6px; right: 6px;
      width: 18px; height: 18px; border-radius: 50%;
      background: var(--color-semantic-danger); color: #fff;
      font-size: 10px; font-weight: 700;
      display: flex; align-items: center; justify-content: center;
    }

    .card-info { padding: var(--space-2) var(--space-3) var(--space-3); display: flex; flex-direction: column; gap: 4px; }
    .card-name { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .card-meta { display: flex; align-items: center; gap: var(--space-2); }
    .card-number { font-size: var(--font-size-xs); color: var(--color-text-muted); font-family: var(--font-family-mono); }
    .card-rarity { font-size: 10px; background: var(--color-surface-sunken); border-radius: var(--radius-sm); padding: 1px 5px; color: var(--color-text-secondary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 80px; }

    .card-price { margin-top: 2px; display: flex; flex-direction: column; }
    .price-value { font-size: var(--font-size-sm); font-weight: var(--font-weight-bold); color: var(--color-brand-primary); }
    .price-source { font-size: 10px; color: var(--color-text-muted); }
    .card-price--none .price-value { font-size: var(--font-size-xs); font-weight: var(--font-weight-regular); color: var(--color-text-disabled); }

    /* ── Pagination ── */
    .pagination { display: flex; align-items: center; justify-content: center; gap: var(--space-1); }
    .page-btn {
      min-width: 34px; height: 34px; display: flex; align-items: center; justify-content: center;
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-base); color: var(--color-text-secondary);
      font-size: var(--font-size-sm); cursor: pointer;
      &:hover:not(:disabled) { background: var(--color-surface-raised); border-color: var(--color-brand-primary); color: var(--color-brand-primary); }
      &--active { background: var(--color-brand-primary); border-color: var(--color-brand-primary); color: #fff; }
      &:disabled { opacity: 0.4; cursor: not-allowed; }
    }
    .page-ellipsis { padding: 0 var(--space-1); color: var(--color-text-muted); }
  `],
})
export class MarketListComponent implements OnInit {
  readonly store = inject(MarketStore);
  private readonly permissions = inject(PermissionStore);

  readonly canSync = computed(() => this.permissions.hasPermission()('cards:sync'));

  /** Toggle badge count ↔ % */
  readonly showPercent = signal(false);

  readonly completionPercent = computed(() => {
    const set = this.store.selectedSet();
    if (!set?.totalCards || set.totalCards === 0) return null;
    return Math.min(100, Math.round((set.cardCount / set.totalCards) * 100));
  });

  readonly SUPERTYPES: { value: string; label: string }[] = [
    { value: 'Pokémon', label: 'Pokémon' },
    { value: 'Trainer', label: 'Dresseur' },
    { value: 'Energy', label: 'Énergie' },
  ];

  readonly RARITIES = [
    'Common', 'Uncommon', 'Rare', 'Rare Holo', 'Rare Holo EX',
    'Rare Holo GX', 'Rare Holo V', 'Rare Holo VMAX', 'Rare Ultra',
    'Rare Secret', 'Promo',
  ];

  readonly hasActiveFilter = computed(() => {
    const f = this.store.filters();
    return !!(f.search || f.rarity || f.supertype);
  });

  readonly visiblePages = computed(() => {
    const total = this.store.totalPages();
    const current = this.store.filters().page;
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages: number[] = [1];
    if (current > 3) pages.push(-1);
    for (let p = Math.max(2, current - 1); p <= Math.min(total - 1, current + 1); p++) pages.push(p);
    if (current < total - 2) pages.push(-1);
    pages.push(total);
    return pages;
  });

  constructor() {
    // Recharger les cartes dès que setId ou filtres changent
    effect(() => {
      const setId = this.store.selectedSetId();
      const filters = this.store.filters();
      if (setId) {
        this.store.loadCards({ setId, filters });
      }
    });
  }

  ngOnInit(): void {
    this.store.loadSets();
  }

  onSetChange(setId: string): void {
    this.store.selectSet(setId);
  }

  onSearch(val: string): void {
    this.store.setFilter({ search: val });
  }

  onFilter(key: 'rarity' | 'supertype', val: string): void {
    this.store.setFilter({ [key]: val });
  }

  resetFilters(): void {
    this.store.setFilter({ search: '', rarity: '', supertype: '' });
  }

  setPage(p: number): void {
    this.store.setPage(p);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  selectCard(card: MarketCardDto): void {
    this.store.selectCard(card);
  }

  isSelected(card: MarketCardDto): boolean {
    return this.store.selectedCard()?.id === card.id;
  }

  formatSource(source: string | null): string {
    if (!source) return '';
    return source.replace('/', ' · ');
  }

  onSyncCatalog(): void {
    this.store.syncCatalog(true);
  }

  onSyncPrices(): void {
    const cardIds = this.store.cards().map(c => c.id);
    this.store.syncPrices(cardIds);
  }

  onSyncPopulation(): void {
    const cardIds = this.store.cards().map(c => c.id);
    this.store.syncPopulation(cardIds);
  }
}
