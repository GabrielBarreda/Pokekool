import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject, computed } from '@angular/core';
import { pipe, switchMap, tap, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  MarketCardDto, PokemonSetDto, MarketFilters, SetFiltersDto,
  CardGradingData, GradingCompanyId, CardGradingApiDto,
  SyncCatalogResultDto, SyncPricesResultDto, SyncPopulationResultDto,
} from './market.models';
import { MarketService } from './market.service';
import { ToastService } from '@shared/services/toast.service';

export interface SyncResult {
  type: 'prices' | 'population';
  pricesUpserted?: number;
  populationsUpserted?: number;
  cardsFailed: number;
  errors: string[] | null;
  completedAt: Date;
  gradedByCompany?: Record<string, number> | null;
  cardsWithoutAnyPrice?: number;
  cardsWithKeyGradesMissing?: number;
}

export interface MarketState {
  sets: PokemonSetDto[];
  selectedSetId: string | null;
  cards: MarketCardDto[];
  availableRarities: string[];
  availableSupertypes: string[];
  totalItems: number;
  totalPages: number;
  filters: MarketFilters;
  selectedCard: MarketCardDto | null;
  gradingData: CardGradingData[];
  gradingLoading: boolean;
  loadingSets: boolean;
  loadingCards: boolean;
  syncingCatalog: boolean;
  syncingPrices: boolean;
  syncingPopulation: boolean;
  lastSyncResult: SyncResult | null;
  error: string | null;
}

const initialFilters: MarketFilters = {
  search: '',
  rarity: '',
  supertype: '',
  page: 1,
  pageSize: 60,
};

const initialState: MarketState = {
  sets: [],
  selectedSetId: null,
  cards: [],
  availableRarities: [],
  availableSupertypes: [],
  totalItems: 0,
  totalPages: 0,
  filters: initialFilters,
  selectedCard: null,
  gradingData: [],
  gradingLoading: false,
  loadingSets: false,
  loadingCards: false,
  syncingCatalog: false,
  syncingPrices: false,
  syncingPopulation: false,
  lastSyncResult: null,
  error: null,
};

export const MarketStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed(store => ({
    selectedSet: computed(() =>
      store.sets().find(s => s.id === store.selectedSetId()) ?? null
    ),
    hasCards: computed(() => store.cards().length > 0),
    isSyncing: computed(() => store.syncingCatalog() || store.syncingPrices() || store.syncingPopulation()),
    seriesGroups: computed(() => {
      const groups = new Map<string, PokemonSetDto[]>();
      for (const s of store.sets()) {
        if (!groups.has(s.series)) groups.set(s.series, []);
        groups.get(s.series)!.push(s);
      }
      return Array.from(groups.entries()).map(([series, sets]) => ({ series, sets }));
    }),
  })),

  withMethods((store, svc = inject(MarketService), toast = inject(ToastService)) => {
    let _loadSets: (value: void) => void;
    let _syncCatalog: (value: { forceRefresh: boolean; setTcgId?: string }) => void;
    let _loadSetFilters: (value: string) => void;
    let loadGrading: (cardId: string) => void;

    _loadSets = rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loadingSets: true, error: null })),
        switchMap(() =>
          svc.getSets().pipe(
            tapResponse({
              next: sets => {
                const sorted = [...sets].sort((a, b) =>
                  (b.releaseDate ?? '').localeCompare(a.releaseDate ?? '')
                );
                const firstSetId = sorted[0]?.id ?? null;
                patchState(store, {
                  sets: sorted,
                  loadingSets: false,
                  selectedSetId: firstSetId,
                });
                if (firstSetId) {
                  _loadSetFilters(firstSetId);
                } else {
                  _syncCatalog({ forceRefresh: false });
                }
              },
              error: () => {
                patchState(store, { loadingSets: false, error: 'Erreur chargement sets' });
                toast.error('Impossible de charger les sets');
              },
            })
          )
        )
      )
    );

    _syncCatalog = rxMethod<{ forceRefresh: boolean; setTcgId?: string }>(
      pipe(
        tap(() => patchState(store, { syncingCatalog: true })),
        switchMap(({ forceRefresh, setTcgId }) =>
          svc.syncCatalog(forceRefresh, setTcgId).pipe(
            tapResponse({
              next: (res: SyncCatalogResultDto) => {
                patchState(store, { syncingCatalog: false });
                _loadSets();
                const label = setTcgId ? `set ${setTcgId}` : 'catalogue';
                if (res.setsImported > 0 || res.cardsImported > 0) {
                  toast.success(`Sync ${label} — ${res.setsImported} sets, ${res.cardsImported} cartes`);
                } else {
                  toast.success(`Sync ${label} — déjà à jour`);
                }
              },
              error: () => {
                patchState(store, { syncingCatalog: false });
                toast.error('Erreur sync catalogue');
              },
            })
          )
        )
      )
    );

    _loadSetFilters = rxMethod<string>(
      pipe(
        switchMap(setId =>
          svc.getSetFilters(setId).pipe(
            tapResponse({
              next: (filters: SetFiltersDto) =>
                patchState(store, {
                  availableRarities: filters.rarities,
                  availableSupertypes: filters.supertypes,
                }),
              error: () => {
                // Non bloquant — les filtres restent tous activés
                patchState(store, { availableRarities: [], availableSupertypes: [] });
              },
            })
          )
        )
      )
    );

    loadGrading = rxMethod<string>(
      pipe(
        switchMap(cardId =>
          svc.getCardGrading(cardId).pipe(
            tapResponse({
              next: (real: CardGradingApiDto[]) => {
                const gradingData: CardGradingData[] = real.map(d => ({
                  cardId,
                  company: d.company as GradingCompanyId,
                  lastUpdated: new Date(d.lastUpdated),
                  entries: d.entries.map(e => ({
                    grade: e.grade,
                    label: e.label,
                    marketPrice: e.marketPrice,
                    lowPrice: e.lowPrice,
                    population: e.population,
                  })),
                }));
                patchState(store, { gradingData, gradingLoading: false });
              },
              error: () => patchState(store, { gradingLoading: false }),
            })
          )
        )
      )
    );

    return {
    loadSets: _loadSets,

    /** setId null = mode "Tous les sets" : catalogue entier, paginé. */
    selectSet(setId: string | null): void {
      patchState(store, {
        selectedSetId: setId,
        filters: { ...initialFilters },
        cards: [],
        selectedCard: null,
        lastSyncResult: null,
        availableRarities: [],
        availableSupertypes: [],
      });
      if (setId) {
        _loadSetFilters(setId);
      } else {
        // Pas de filtres rarité/supertype pré-calculés en mode "Tous" — tous activés.
        patchState(store, { availableRarities: [], availableSupertypes: [] });
      }
    },

    loadCards: rxMethod<{ setId: string | null; filters: MarketFilters }>(
      pipe(
        debounceTime(300),
        distinctUntilChanged((a, b) =>
          a.setId === b.setId && JSON.stringify(a.filters) === JSON.stringify(b.filters)
        ),
        tap(() => patchState(store, { loadingCards: true, error: null })),
        switchMap(({ setId, filters }) =>
          svc.getMarketCards(setId, filters).pipe(
            tapResponse({
              next: result =>
                patchState(store, {
                  cards: result.items,
                  totalItems: result.totalItems,
                  totalPages: result.totalPages,
                  loadingCards: false,
                }),
              error: () => {
                patchState(store, { loadingCards: false, error: 'Erreur chargement cartes' });
                toast.error('Impossible de charger les cartes');
              },
            })
          )
        )
      )
    ),

    syncCatalog: _syncCatalog,

    syncPrices: rxMethod<{ setId?: string; cardIds?: string[]; stalenessHours?: number }>(
      pipe(
        tap(() => patchState(store, { syncingPrices: true, lastSyncResult: null })),
        switchMap(opts =>
          svc.syncPrices(opts).pipe(
            tapResponse({
              next: (res: SyncPricesResultDto) => {
                const result: SyncResult = {
                  type: 'prices',
                  pricesUpserted: res.pricesUpserted,
                  cardsFailed: res.cardsFailed,
                  errors: res.errors,
                  completedAt: new Date(),
                  gradedByCompany: res.gradedByCompany,
                  cardsWithoutAnyPrice: res.cardsWithoutAnyPrice,
                  cardsWithKeyGradesMissing: res.cardsWithKeyGradesMissing,
                };
                patchState(store, { syncingPrices: false, lastSyncResult: result });
                if (res.cardsFailed === 0) {
                  toast.success(`Prix sync — ${res.pricesUpserted} mis à jour`);
                } else {
                  toast.error(`Prix sync — ${res.cardsFailed} échec(s)`);
                }
              },
              error: () => {
                patchState(store, { syncingPrices: false });
                toast.error('Erreur lors du sync des prix');
              },
            })
          )
        )
      )
    ),

    syncPopulation: rxMethod<string[]>(
      pipe(
        tap(() => patchState(store, { syncingPopulation: true, lastSyncResult: null })),
        switchMap(cardIds =>
          svc.syncPopulation(cardIds).pipe(
            tapResponse({
              next: (res: SyncPopulationResultDto) => {
                const result: SyncResult = {
                  type: 'population',
                  populationsUpserted: res.populationsUpserted,
                  cardsFailed: res.cardsFailed,
                  errors: res.errors,
                  completedAt: new Date(),
                };
                patchState(store, { syncingPopulation: false, lastSyncResult: result });
                if (res.cardsFailed === 0) {
                  toast.success(`Population sync — ${res.populationsUpserted} mis à jour`);
                } else {
                  toast.error(`Population sync — ${res.cardsFailed} échec(s)`);
                }
              },
              error: () => {
                patchState(store, { syncingPopulation: false });
                toast.error('Erreur lors du sync population');
              },
            })
          )
        )
      )
    ),

    loadGrading,

    dismissSyncResult(): void {
      patchState(store, { lastSyncResult: null });
    },

    setFilter(partial: Partial<MarketFilters>): void {
      patchState(store, {
        filters: { ...store.filters(), ...partial, page: 1 },
      });
    },

    setPage(page: number): void {
      patchState(store, { filters: { ...store.filters(), page } });
    },

    selectCard(card: MarketCardDto | null): void {
      patchState(store, { selectedCard: card });
      if (card) {
        patchState(store, { gradingData: [], gradingLoading: true });
        loadGrading(card.id);
      } else {
        patchState(store, { gradingData: [], gradingLoading: false });
      }
    },

    getGradingForCompany(companyId: GradingCompanyId) {
      return store.gradingData().find(d => d.company === companyId) ?? null;
    },
  };
  })
);
