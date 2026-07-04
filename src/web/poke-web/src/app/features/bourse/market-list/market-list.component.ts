import {
  Component, inject, OnInit, ChangeDetectionStrategy, signal, computed, effect,
  ViewChild, ElementRef, Injector, afterNextRender,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MarketStore } from '../market.store';
import { MarketCardDto } from '../market.models';
import { CardDetailComponent } from '../card-detail/card-detail.component';
import { SyncTickerComponent } from '../sync-ticker/sync-ticker.component';
import { ScheduleSyncModalComponent } from '../schedule-sync-modal/schedule-sync-modal.component';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { PermissionStore } from '@core/auth/permission.store';
import { ThemeStore } from '@core/theme/theme.store';
import { SyncScheduleDto } from '../market.models';
import { ToastService } from '@shared/services/toast.service';

@Component({
  selector: 'app-market-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, CardDetailComponent, SyncTickerComponent, ScheduleSyncModalComponent, PageHeaderComponent, SpinnerComponent, EmptyStateComponent],
  template: `
    <div class="page"
         [class.page--detail-open]="!!store.selectedCard() && !themeStore.isPokemon()"
         [class.theme-pokemon]="themeStore.isPokemon()">
      <!-- Header, sélecteur de set, panneau sync et filtres : masqués tant
           que le détail carte est ouvert pour ne garder à gauche que la
           liste des autres cartes du set (thème par défaut) ou pour laisser
           le classeur respirer (thème Pokémon). Le choix du thème se fait
           désormais dans Paramètres > Apparence (cf. AppearanceSettingsComponent). -->
      @if (!store.selectedCard()) {
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

            <!-- Programmer sync global -->
            <button class="sync-btn sync-btn--schedule"
                    [disabled]="store.isSyncing()"
                    (click)="showScheduleModal.set(true)">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
              </svg>
              Programmer (global)
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
                    {{ result.pricesUpserted }} prix
                    @for (entry of getGradedEntries(result); track entry.key) {
                      &nbsp;·&nbsp;{{ entry.key }} {{ entry.value }}
                    }
                    @if ((result.cardsFailed ?? 0) > 0) {
                      &nbsp;·&nbsp;{{ result.cardsFailed }} échec(s)
                    }
                  } @else {
                    {{ result.populationsUpserted }} populations mises à jour · {{ result.cardsFailed }} échec(s)
                  }
                </span>
                <span class="sync-result__time">{{ result.completedAt | date:'HH:mm:ss' }}</span>
                <button class="sync-result__dismiss" (click)="store.dismissSyncResult()">×</button>
              </div>
              @if (result.type === 'prices' && ((result.cardsWithKeyGradesMissing ?? 0) > 0 || (result.cardsWithoutAnyPrice ?? 0) > 0)) {
                <div class="sync-result__gaps">
                  <span>⚠</span>
                  @if ((result.cardsWithKeyGradesMissing ?? 0) > 0) {
                    <span>{{ result.cardsWithKeyGradesMissing }} cartes sans PSA 7-10</span>
                  }
                  @if ((result.cardsWithKeyGradesMissing ?? 0) > 0 && (result.cardsWithoutAnyPrice ?? 0) > 0) {
                    <span>·</span>
                  }
                  @if ((result.cardsWithoutAnyPrice ?? 0) > 0) {
                    <span>{{ result.cardsWithoutAnyPrice }} cartes sans prix</span>
                  }
                </div>
              }
              @if (result.errors && result.errors.length > 0) {
                <details class="sync-errors">
                  <summary>{{ result.errors.length }} erreur(s) détaillées</summary>
                  <ul>
                    @for (e of result.errors; track e) { <li>{{ e }}</li> }
                  </ul>
                </details>
              }
              @if (result.type === 'prices' && result.pricesUpserted === 0 && (result.cardsFailed ?? 0) === 0) {
                <div class="sync-result__fresh">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                  </svg>
                  <span>Les données semblent récentes — forcer la synchronisation ?</span>
                  <button class="sync-result__force-btn" (click)="onForceSyncPrices()">Forcer</button>
                </div>
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
      }

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
          <!-- Ticker SSE pendant sync -->
          @if (store.syncingPrices()) {
            <app-sync-ticker />
          }

          @if (themeStore.isPokemon()) {
            <!-- ═══════════ Classeur Pokémon (thème pokemon) — double page ═══════════
                 Pochettes façon vrai classeur (transparentes, sans étiquette —
                 le nom est déjà sur le scan de la carte). Quand une carte est
                 sélectionnée : le classeur reste visible mais estompé derrière
                 (sa pochette source affichée vide), et la carte tournoie en 3D
                 vers le premier plan (cf. selectCard/closeDetail/playHeroSpin). -->
            <ng-template #pocketsGrid let-page>
              @for (card of page; track card.id) {
                <button class="binder-pocket"
                        [attr.data-pocket-id]="card.id"
                        [class.binder-pocket--holo]="isHoloRarity(card.rarity)"
                        [class.binder-pocket--source]="isSelected(card)"
                        (click)="selectCard(card)"
                        (mousemove)="onHoloMove($event)"
                        (mouseleave)="onHoloLeave($event)">
                  <div class="binder-pocket__sheen" aria-hidden="true"></div>
                  <div class="binder-pocket__img-wrap">
                    @if (card.imageSmall) {
                      <img [src]="card.imageSmall" [alt]="card.name" class="binder-pocket__img" loading="lazy" />
                    } @else {
                      <div class="binder-pocket__placeholder">
                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.35">
                          <rect x="3" y="3" width="18" height="18" rx="2"/>
                          <circle cx="12" cy="12" r="3"/>
                        </svg>
                      </div>
                    }
                    @if (card.alertCount > 0) {
                      <span class="alert-dot">{{ card.alertCount }}</span>
                    }
                  </div>
                </button>
              }
              @for (i of emptyPocketRange(page.length); track i) {
                <div class="binder-pocket binder-pocket--empty" aria-hidden="true"></div>
              }
            </ng-template>

            <div class="pokemon-stage" [class.pokemon-stage--detail]="!!store.selectedCard()">
              <div class="pokemon-stage__binder-zone" [class.pokemon-stage__binder-zone--solo]="!!store.selectedCard()">
                <div class="binder" [class.binder--dimmed]="!!store.selectedCard()">
                  @if (binderDetailPage(); as soloPage) {
                    <!-- Détail ouvert : une seule page (celle de la carte, avec
                         sa pochette vide) — l'espace gagné revient aux données. -->
                    <div class="binder-page binder-page--solo">
                      <div class="binder-pockets">
                        <ng-container [ngTemplateOutlet]="pocketsGrid" [ngTemplateOutletContext]="{ $implicit: soloPage }" />
                      </div>
                    </div>
                  } @else {
                    <div class="binder-spread"
                         [class.binder-spread--out-next]="binderAnimState() === 'out' && binderFlipDir() === 'next'"
                         [class.binder-spread--out-prev]="binderAnimState() === 'out' && binderFlipDir() === 'prev'"
                         [class.binder-spread--in-next]="binderAnimState() === 'in' && binderFlipDir() === 'next'"
                         [class.binder-spread--in-prev]="binderAnimState() === 'in' && binderFlipDir() === 'prev'">
                      <div class="binder-page binder-page--left">
                        <div class="binder-pockets">
                          <ng-container [ngTemplateOutlet]="pocketsGrid" [ngTemplateOutletContext]="{ $implicit: binderLeftPage() }" />
                        </div>
                      </div>
                      <div class="binder-spine" aria-hidden="true">
                        <div class="binder-spine__rings"></div>
                      </div>
                      <div class="binder-page binder-page--right">
                        <div class="binder-pockets">
                          <ng-container [ngTemplateOutlet]="pocketsGrid" [ngTemplateOutletContext]="{ $implicit: binderRightPage() }" />
                        </div>
                      </div>
                    </div>

                    <div class="binder-nav">
                      <button class="binder-nav__btn" type="button"
                              [disabled]="binderSpreadIndex() === 0"
                              (click)="flipBinder('prev')" aria-label="Double page précédente du classeur">‹</button>
                      <span class="binder-nav__page">Double page {{ binderSpreadIndex() + 1 }} / {{ binderSpreadCount() }}</span>
                      <button class="binder-nav__btn" type="button"
                              [disabled]="binderSpreadIndex() >= binderSpreadCount() - 1"
                              (click)="flipBinder('next')" aria-label="Double page suivante du classeur">›</button>
                    </div>
                  }
                </div>

                @if (store.selectedCard(); as card) {
                  <!-- Carte au premier plan : tourbillon d'entrée/sortie (WAAPI)
                       + inclinaison 3D façon holographique au survol. -->
                  <div class="pokemon-hero-wrap">
                    <div class="pokemon-hero" #heroCard
                         (mousemove)="onHeroTilt($event)"
                         (mouseleave)="onHeroTiltLeave($event)">
                      <img [src]="card.imageLarge ?? card.imageSmall" [alt]="card.name" class="pokemon-hero__img" />
                    </div>
                  </div>
                }
              </div>

              @if (store.selectedCard(); as card) {
                <app-card-detail class="pokemon-stage__data"
                  [card]="card"
                  [gradingData]="store.gradingData()"
                  [gradingLoading]="store.gradingLoading()"
                  (closed)="closeDetail()" />
              }
            </div>
          } @else {
            <!-- ═══════════ Grille par défaut ═══════════ -->
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
        }
      </div>
    </div>

    <!-- Modal programmer sync -->
    @if (showScheduleModal()) {
      <app-schedule-sync-modal
        (closed)="showScheduleModal.set(false)"
        (scheduled)="onScheduled($event)" />
    }

    <!-- Card detail drawer (thème par défaut uniquement — en thème Pokémon
         le détail est rendu plein cadre à la place du classeur, cf. plus haut) -->
    @if (store.selectedCard() && !themeStore.isPokemon()) {
      <app-card-detail
        [card]="store.selectedCard()!"
        [gradingData]="store.gradingData()"
        [gradingLoading]="store.gradingLoading()"
        (closed)="closeDetail()" />
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-5); }
    /* Quand le détail carte est ouvert (drawer ~80% à droite), la page se
       resserre en une liste verticale d'une carte de large : header, sélecteur
       de set, panneau sync et filtres sont masqués (cf. template), il ne reste
       que la grille de cartes. Le scroll vertical normal de la page permet de
       parcourir toutes les cartes du set dans l'ordre. */
    .page--detail-open {
      width: 200px;
      max-width: 100%;
    }
    @media (max-width: 960px) {
      .page--detail-open { width: 100%; }
    }
    .page--detail-open .card-grid { grid-template-columns: 1fr; }
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
    .sync-btn--schedule {
      background: var(--color-surface-raised);
      border-color: var(--color-border-default);
      color: var(--color-text-muted);
      &:not(:disabled):hover { border-color: var(--color-semantic-info); color: var(--color-semantic-info); }
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
    .sync-result__gaps {
      display: flex; align-items: center; gap: var(--space-2);
      margin-top: var(--space-1);
      font-size: var(--font-size-xs);
      opacity: 0.85;
    }
    .sync-result__fresh {
      display: flex; align-items: center; gap: var(--space-2);
      margin-top: var(--space-2);
      padding-top: var(--space-2);
      border-top: 1px solid currentColor;
      opacity: 0.8;
      font-size: var(--font-size-xs);
      svg { flex-shrink: 0; }
    }
    .sync-result__force-btn {
      all: unset; cursor: pointer; margin-left: auto;
      padding: 2px 8px;
      border-radius: var(--radius-md);
      border: 1px solid currentColor;
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      &:hover { opacity: 1; background: rgba(0,0,0,0.08); }
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
    .pagination { display: flex; align-items: center; justify-content: center; gap: var(--space-1); flex-wrap: wrap; }
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

    /* ════════════════════════════════════════════════════════════
       THÈME POKÉMON — Classeur à pochettes (binder)
       ════════════════════════════════════════════════════════════ */

    /* Scène : classeur (estompé quand une carte est sélectionnée, avec la
       carte en premier plan par-dessus) à gauche, données de gradation à
       droite — le classeur reste visible, il n'est jamais démonté du DOM. */
    .pokemon-stage {
      display: flex;
      align-items: flex-start;
      gap: var(--space-6);
    }
    .pokemon-stage--detail { align-items: stretch; }

    .pokemon-stage__binder-zone {
      position: relative;
      flex: 1 1 480px;
      min-width: 0;
      display: flex;
      justify-content: center;
      transition: flex-basis var(--duration-normal) var(--easing-standard);
    }
    /* Détail ouvert : le classeur n'affiche plus qu'une page → il se réduit
       à sa largeur naturelle, l'espace gagné revient à la zone de données. */
    .pokemon-stage__binder-zone--solo { flex: 0 0 auto; }
    .pokemon-stage__data { flex: 1 1 480px; min-width: 0; align-self: stretch; }

    .binder {
      position: relative;
      max-width: 1180px;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-4);
      padding: var(--space-6);
      border-radius: var(--radius-2xl);
      background:
        radial-gradient(circle at 15% 10%, rgba(255,255,255,0.08), transparent 45%),
        repeating-linear-gradient(135deg, rgba(255,255,255,0.03) 0px, rgba(255,255,255,0.03) 2px, transparent 2px, transparent 7px),
        linear-gradient(155deg, #37220f 0%, #24140a 45%, #150c07 100%);
      border: 1px solid rgba(0,0,0,0.55);
      box-shadow:
        0 32px 60px -12px rgba(0,0,0,0.55),
        0 12px 24px rgba(0,0,0,0.35),
        inset 0 1px 0 rgba(255,255,255,0.09),
        inset 0 0 0 1px rgba(0,0,0,0.35),
        inset 0 0 36px rgba(0,0,0,0.35);
      transition: opacity var(--duration-normal) var(--easing-standard);
    }
    /* Liseré cousu en retrait du bord — détail "maroquinerie" de la couverture. */
    .binder::before {
      content: '';
      position: absolute;
      inset: 11px;
      border: 1px dashed rgba(255,255,255,0.14);
      border-radius: calc(var(--radius-2xl) - 9px);
      pointer-events: none;
    }
    /* Ombre portée au sol pour détacher visuellement le classeur de la page. */
    .binder::after {
      content: '';
      position: absolute;
      left: 8%;
      right: 8%;
      bottom: -16px;
      height: 28px;
      background: radial-gradient(ellipse at center, rgba(0,0,0,0.45), transparent 72%);
      filter: blur(5px);
      pointer-events: none;
      z-index: -1;
    }
    /* Classeur en arrière-plan pendant le détail : estompé et non interactif. */
    .binder--dimmed { opacity: 0.25; filter: saturate(0.7); pointer-events: none; }

    /* Pochette source de la carte affichée en premier plan : image masquée
       pour donner l'impression que la carte est "sortie" de sa pochette. */
    .binder-pocket--source .binder-pocket__img-wrap { opacity: 0; }

    /* Carte en premier plan (hero) : superposée au classeur, tournoie à
       l'ouverture/fermeture (WAAPI, cf. playHeroSpin) et s'incline avec la
       souris comme une carte holographique (cf. onHeroTilt). */
    .pokemon-hero-wrap {
      position: absolute;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      perspective: 1600px;
      pointer-events: none;
    }
    .pokemon-hero {
      pointer-events: auto;
      width: min(62%, 300px);
      aspect-ratio: 63/88;
      border-radius: var(--radius-lg);
      cursor: pointer;
      transform-style: preserve-3d;
      transition: transform 120ms ease-out;
      box-shadow: 0 20px 50px rgba(0,0,0,0.55), 0 4px 14px rgba(0,0,0,0.4);
    }
    .pokemon-hero__img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
      border-radius: var(--radius-lg);
      backface-visibility: hidden;
    }
    @media (prefers-reduced-motion: reduce) {
      .pokemon-hero { transition: none; }
    }

    /* Double page façon classeur ouvert : page gauche | tranche | page droite. */
    .binder-spread {
      display: flex;
      align-items: stretch;
      justify-content: center;
      perspective: 2200px;
    }
    .binder-page { display: flex; align-items: center; }

    /* Pochettes à taille plafonnée : sans cette limite, la grille (1fr) prend
       toute la largeur disponible et les cartes deviennent énormes. */
    .binder-pockets {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 168px));
      gap: var(--space-3);
    }

    /* Tranche centrale du classeur */
    .binder-spine {
      width: 22px;
      flex-shrink: 0;
      align-self: stretch;
      margin: 0 var(--space-3);
      border-radius: 3px;
      background: linear-gradient(90deg, rgba(0,0,0,0.55), rgba(0,0,0,0.15) 25%, rgba(0,0,0,0.15) 75%, rgba(0,0,0,0.55));
      box-shadow: inset 0 0 14px rgba(0,0,0,0.65), var(--shadow-md);
      position: relative;
    }
    .binder-spine__rings {
      position: absolute;
      inset: 10px 7px;
      background-image: repeating-linear-gradient(180deg, rgba(255,255,255,0.18) 0, rgba(255,255,255,0.18) 3px, transparent 3px, transparent 24px);
      border-radius: 2px;
    }

    @keyframes binderFlipOutNext { from { transform: rotateY(0deg);   opacity: 1;    } to { transform: rotateY(-32deg); opacity: 0.2; } }
    @keyframes binderFlipInNext  { from { transform: rotateY(32deg);  opacity: 0.2; } to { transform: rotateY(0deg);   opacity: 1;    } }
    @keyframes binderFlipOutPrev { from { transform: rotateY(0deg);   opacity: 1;    } to { transform: rotateY(32deg);  opacity: 0.2; } }
    @keyframes binderFlipInPrev  { from { transform: rotateY(-32deg); opacity: 0.2; } to { transform: rotateY(0deg);   opacity: 1;    } }

    .binder-spread--out-next { transform-origin: left center;  animation: binderFlipOutNext 200ms ease-in forwards; }
    .binder-spread--in-next  { transform-origin: right center; animation: binderFlipInNext  200ms ease-out forwards; }
    .binder-spread--out-prev { transform-origin: right center; animation: binderFlipOutPrev 200ms ease-in forwards; }
    .binder-spread--in-prev  { transform-origin: left center;  animation: binderFlipInPrev  200ms ease-out forwards; }

    .binder-pocket {
      all: unset;
      position: relative;
      display: block;
      cursor: pointer;
      border-radius: 4px;
      border: 1px solid rgba(255,255,255,0.18);
      overflow: hidden;
      /* Pochette plastique transparente : quasi pas de marge interne, la
         carte remplit l'emplacement comme dans un vrai classeur. */
      transition: border-color var(--duration-fast) var(--easing-standard),
                  box-shadow var(--duration-fast) var(--easing-standard);
      &:hover, &:focus-visible {
        border-color: rgba(255,255,255,0.5);
        box-shadow: 0 0 0 2px rgba(255,255,255,0.14), var(--shadow-lg);
        outline: none;
      }
      &--empty {
        border-style: dashed;
        border-color: rgba(255,255,255,0.08);
        background: rgba(0,0,0,0.15);
        aspect-ratio: 63/88;
        cursor: default;
      }
    }
    .binder-pocket__sheen {
      position: absolute;
      inset: 0;
      background: linear-gradient(115deg, transparent 30%, rgba(255,255,255,0.18) 45%, transparent 60%);
      pointer-events: none;
      opacity: 0.8;
      z-index: 1;
    }
    .binder-pocket__img-wrap {
      position: relative;
      aspect-ratio: 63/88;
      overflow: hidden;
      background: var(--color-neutral-800);
    }

    /* ── Holo (cartes rares) ──
       Reflet arc-en-ciel qui suit le pointeur (--holo-x/--holo-y, mis à
       jour en JS via onHoloMove/onHoloLeave), visible au survol seulement. */
    .binder-pocket--holo { --holo-x: 50%; --holo-y: 50%; }
    .binder-pocket--holo .binder-pocket__img-wrap::after {
      content: '';
      position: absolute;
      inset: 0;
      background: linear-gradient(
        115deg,
        transparent 20%,
        rgba(255, 0, 150, 0.35) 32%,
        rgba(255, 225, 0, 0.35) 40%,
        rgba(0, 255, 200, 0.35) 48%,
        rgba(120, 0, 255, 0.35) 56%,
        transparent 68%
      );
      background-size: 220% 220%;
      background-position: var(--holo-x) var(--holo-y);
      mix-blend-mode: color-dodge;
      opacity: 0;
      transition: opacity var(--duration-fast) var(--easing-standard);
      pointer-events: none;
    }
    .binder-pocket--holo:hover .binder-pocket__img-wrap::after {
      opacity: 0.85;
    }
    @media (prefers-reduced-motion: reduce) {
      .binder-pocket--holo .binder-pocket__img-wrap::after { transition: none; }
    }
    .binder-pocket__img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .binder-pocket__placeholder { display: flex; align-items: center; justify-content: center; width: 100%; height: 100%; color: rgba(255,255,255,0.4); }

    .binder-nav { display: flex; align-items: center; justify-content: center; gap: var(--space-4); padding-top: var(--space-2); }
    .binder-nav__btn {
      width: 34px; height: 34px; border-radius: var(--radius-full);
      border: 1px solid rgba(255,255,255,0.2);
      background: rgba(255,255,255,0.06);
      color: #fff; font-size: 18px; line-height: 1; cursor: pointer;
      display: flex; align-items: center; justify-content: center;
      transition: background var(--duration-fast), opacity var(--duration-fast);
      &:hover:not(:disabled) { background: rgba(255,255,255,0.16); }
      &:disabled { opacity: 0.3; cursor: not-allowed; }
    }
    .binder-nav__page { font-size: var(--font-size-xs); color: rgba(255,255,255,0.75); font-family: var(--font-family-mono); }

    @media (prefers-reduced-motion: reduce) {
      .binder-spread { animation: none !important; }
      .binder-pocket { transition: none !important; }
      .pokemon-hero { transition: none !important; }
    }

    @media (max-width: 960px) {
      .pokemon-stage--detail { flex-direction: column; }
      /* Sous ~960px, la double page ne tient plus côte à côte : on repasse
         en pile verticale (page gauche puis page droite). */
      .binder-spread { flex-direction: column; align-items: center; gap: var(--space-4); }
      .binder-spine { display: none; }
      .binder-pockets { grid-template-columns: repeat(3, minmax(0, 140px)); }
      .pokemon-hero { width: min(70%, 220px); }
    }
  `],
})
export class MarketListComponent implements OnInit {
  readonly store = inject(MarketStore);
  readonly themeStore = inject(ThemeStore);
  private readonly permissions = inject(PermissionStore);
  private readonly toast = inject(ToastService);
  private readonly injector = inject(Injector);
  readonly showScheduleModal = signal(false);

  // ── Thème Pokémon — classeur binder (double page) ────────────────────────
  readonly BINDER_PAGE_SIZE = 9; // pochettes par page physique (3×3)
  readonly binderSpreadIndex = signal(0);
  readonly binderAnimState = signal<'idle' | 'out' | 'in'>('idle');
  readonly binderFlipDir = signal<'next' | 'prev'>('next');

  /** Carte en premier plan (tourbillon d'entrée/sortie + inclinaison 3D). */
  @ViewChild('heroCard') heroCardRef?: ElementRef<HTMLElement>;
  readonly heroAnimating = signal(false);

  /** Pages physiques (3×3), chunkées depuis les cartes chargées. */
  readonly binderPages = computed(() => {
    const cards = this.store.cards();
    if (cards.length === 0) return [[] as MarketCardDto[]];
    const pages: MarketCardDto[][] = [];
    for (let i = 0; i < cards.length; i += this.BINDER_PAGE_SIZE) {
      pages.push(cards.slice(i, i + this.BINDER_PAGE_SIZE));
    }
    return pages;
  });

  /** Nombre de doubles pages (2 pages physiques par vue, comme un vrai classeur ouvert). */
  readonly binderSpreadCount = computed(() => Math.max(1, Math.ceil(this.binderPages().length / 2)));

  readonly binderLeftPage = computed(() => this.binderPages()[this.binderSpreadIndex() * 2] ?? []);
  readonly binderRightPage = computed(() => this.binderPages()[this.binderSpreadIndex() * 2 + 1] ?? []);

  /** Page physique contenant la carte sélectionnée — affichée seule (avec sa
   *  pochette vide) à la place de la double page pendant le détail. */
  readonly binderDetailPage = computed(() => {
    const card = this.store.selectedCard();
    if (!card) return null;
    return this.binderPages().find(p => p.some(c => c.id === card.id)) ?? null;
  });

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

    // Revenir à la première page du classeur quand le set ou les filtres
    // changent (nouvelle liste de cartes servie par le back).
    effect(() => {
      this.store.selectedSetId();
      this.store.filters();
      this.binderSpreadIndex.set(0);
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
    if (!this.themeStore.isPokemon()) {
      this.store.selectCard(card);
      return;
    }
    // Position de départ (pochette cliquée) capturée AVANT le changement
    // d'état, pendant que le classeur est encore pleinement affiché.
    const startRect = this.pocketEl(card.id)?.getBoundingClientRect() ?? null;
    this.store.selectCard(card); // classeur estompé, pochette source vidée, hero monté
    if (!startRect) return;

    // Attend qu'Angular ait rendu le hero card (afterNextRender = après le
    // prochain cycle de rendu/peinture) avant de jouer le tourbillon d'entrée.
    afterNextRender(() => {
      const hero = this.heroCardRef?.nativeElement;
      if (hero) this.playHeroSpin(hero, startRect, 'enter');
    }, { injector: this.injector });
  }

  closeDetail(): void {
    const card = this.store.selectedCard();
    if (!this.themeStore.isPokemon() || !card) {
      this.store.selectCard(null);
      return;
    }
    const hero = this.heroCardRef?.nativeElement;
    const pocket = this.pocketEl(card.id);
    if (!hero || !pocket) {
      this.store.selectCard(null);
      return;
    }
    // Le classeur (et sa pochette) est toujours dans le DOM (juste estompé),
    // donc on peut jouer le tourbillon de sortie tout de suite, puis ne
    // démonter le hero / réactiver le classeur qu'une fois l'animation finie.
    const endRect = pocket.getBoundingClientRect();
    this.playHeroSpin(hero, endRect, 'exit').finally(() => {
      this.store.selectCard(null);
    });
  }

  isSelected(card: MarketCardDto): boolean {
    return this.store.selectedCard()?.id === card.id;
  }

  /**
   * Anime la carte en premier plan (hero) entre sa position naturelle
   * (centrée, grande) et le rectangle de sa pochette dans le classeur —
   * translation + mise à l'échelle + rotation Y complète (360°), façon
   * carte qui tournoie en sortant/rentrant dans sa pochette. Utilise la Web
   * Animations API plutôt que la View Transition API pour garder le plein
   * contrôle sur les keyframes intermédiaires (le tourbillon).
   */
  private playHeroSpin(hero: HTMLElement, pocketRect: DOMRect, mode: 'enter' | 'exit'): Promise<void> {
    hero.style.transform = ''; // repart d'une base neutre (efface une éventuelle inclinaison 3D résiduelle)

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reducedMotion) return Promise.resolve();

    const heroRect = hero.getBoundingClientRect();
    const dx = pocketRect.left + pocketRect.width / 2 - (heroRect.left + heroRect.width / 2);
    const dy = pocketRect.top + pocketRect.height / 2 - (heroRect.top + heroRect.height / 2);
    const scale = Math.max(0.05, pocketRect.width / heroRect.width);
    const midScale = (scale + 1) / 2;

    const enterFrames: Keyframe[] = [
      { transform: `translate(${dx}px, ${dy}px) scale(${scale}) rotateY(0deg)`, opacity: 0.5, offset: 0 },
      { transform: `translate(${dx * 0.35}px, ${dy * 0.35}px) scale(${midScale}) rotateY(180deg)`, opacity: 1, offset: 0.55 },
      { transform: 'translate(0px, 0px) scale(1) rotateY(360deg)', opacity: 1, offset: 1 },
    ];
    const exitFrames: Keyframe[] = [
      { transform: 'translate(0px, 0px) scale(1) rotateY(0deg)', opacity: 1, offset: 0 },
      { transform: `translate(${dx * 0.35}px, ${dy * 0.35}px) scale(${midScale}) rotateY(-180deg)`, opacity: 1, offset: 0.45 },
      { transform: `translate(${dx}px, ${dy}px) scale(${scale}) rotateY(-360deg)`, opacity: 0.35, offset: 1 },
    ];

    this.heroAnimating.set(true);
    const anim = hero.animate(mode === 'enter' ? enterFrames : exitFrames, {
      duration: mode === 'enter' ? 680 : 560,
      easing: mode === 'enter' ? 'cubic-bezier(0.22, 0.68, 0.24, 1)' : 'cubic-bezier(0.55, 0, 0.75, 0.1)',
      fill: 'both',
    });

    // .then(fulfilled, rejected) plutôt que .catch() pour que le type reste
    // Promise<void> (le .catch() seul renvoie Promise<Animation | void>,
    // puisque `anim.finished` résout en Animation).
    return anim.finished
      .then(
        () => undefined,
        () => undefined,
      )
      .finally(() => {
        this.heroAnimating.set(false);
        anim.cancel(); // relâche l'effet WAAPI — l'inclinaison 3D au survol reprend la main
      });
  }

  /** Inclinaison 3D de la carte au premier plan, façon carte holographique
   *  tenue en main — ignorée pendant le tourbillon d'entrée/sortie. */
  onHeroTilt(event: MouseEvent): void {
    if (this.heroAnimating()) return;
    const el = event.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    const px = (event.clientX - rect.left) / rect.width - 0.5;
    const py = (event.clientY - rect.top) / rect.height - 0.5;
    const maxTilt = 14;
    el.style.transform = `rotateX(${(-py * maxTilt).toFixed(2)}deg) rotateY(${(px * maxTilt).toFixed(2)}deg) scale(1.03)`;
  }

  onHeroTiltLeave(event: MouseEvent): void {
    if (this.heroAnimating()) return;
    (event.currentTarget as HTMLElement).style.transform = '';
  }

  /** Pochettes vides pour compléter visuellement la page à BINDER_PAGE_SIZE. */
  emptyPocketRange(filledCount: number): number[] {
    const remaining = Math.max(0, this.BINDER_PAGE_SIZE - filledCount);
    return Array.from({ length: remaining }, (_, i) => i);
  }

  /** Tourne la double page du classeur avec une animation 3D (désactivée si
   *  prefers-reduced-motion). */
  flipBinder(direction: 'next' | 'prev'): void {
    const total = this.binderSpreadCount();
    const target = direction === 'next'
      ? Math.min(this.binderSpreadIndex() + 1, total - 1)
      : Math.max(this.binderSpreadIndex() - 1, 0);
    if (target === this.binderSpreadIndex()) return;

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reducedMotion) {
      this.binderSpreadIndex.set(target);
      return;
    }

    this.binderFlipDir.set(direction);
    this.binderAnimState.set('out');
    window.setTimeout(() => {
      this.binderSpreadIndex.set(target);
      this.binderAnimState.set('in');
      window.setTimeout(() => this.binderAnimState.set('idle'), 220);
    }, 200);
  }

  private pocketEl(cardId: string): HTMLElement | null {
    return document.querySelector<HTMLElement>(`[data-pocket-id="${cardId}"]`);
  }

  // ── Holo (cartes rares) ──
  private readonly HOLO_RARITIES = new Set([
    'Rare Holo', 'Rare Holo EX', 'Rare Holo GX', 'Rare Holo V', 'Rare Holo VMAX',
    'Rare Ultra', 'Rare Secret',
  ]);

  isHoloRarity(rarity: string | null): boolean {
    return !!rarity && this.HOLO_RARITIES.has(rarity);
  }

  onHoloMove(event: MouseEvent): void {
    const el = event.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    const x = ((event.clientX - rect.left) / rect.width) * 100;
    const y = ((event.clientY - rect.top) / rect.height) * 100;
    el.style.setProperty('--holo-x', `${x}%`);
    el.style.setProperty('--holo-y', `${y}%`);
  }

  onHoloLeave(event: MouseEvent): void {
    const el = event.currentTarget as HTMLElement;
    el.style.setProperty('--holo-x', '50%');
    el.style.setProperty('--holo-y', '50%');
  }

  getGradedEntries(result: import('../market.store').SyncResult): { key: string; value: number }[] {
    if (!result.gradedByCompany) return [];
    return Object.entries(result.gradedByCompany).map(([key, value]) => ({ key, value }));
  }

  formatSource(source: string | null): string {
    if (!source) return '';
    return source.replace('/', ' · ');
  }

  onSyncCatalog(): void {
    const set = this.store.selectedSet();
    // Sync du set sélectionné (force refresh pour récupérer les cartes manquantes)
    this.store.syncCatalog({ forceRefresh: true, setTcgId: set?.tcgId ?? undefined });
  }

  onSyncPrices(): void {
    const set = this.store.selectedSet();
    // Sync de toutes les cartes du set (filtre de fraîcheur 24h par défaut)
    this.store.syncPrices({ setId: set?.id ?? undefined });
  }

  onForceSyncPrices(): void {
    const set = this.store.selectedSet();
    // Force sync : bypass le filtre de fraîcheur (stalenessHours=0)
    this.store.syncPrices({ setId: set?.id ?? undefined, stalenessHours: 0 });
  }

  onSyncPopulation(): void {
    const cardIds = this.store.cards().map(c => c.id);
    this.store.syncPopulation(cardIds);
  }

  onScheduled(dto: SyncScheduleDto): void {
    const at = dto.nextRunAt
      ? new Date(dto.nextRunAt).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
      : dto.scheduledTime ?? '';
    const recur = dto.isRecurring ? ' (récurrent)' : '';
    this.toast.success(`Sync planifié à ${at}${recur}`);
  }
}
