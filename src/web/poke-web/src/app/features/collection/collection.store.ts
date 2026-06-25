import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject, computed } from '@angular/core';
import { pipe, switchMap, tap, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  CollectionCardDto,
  CollectionFilters,
  CollectionStatsDto,
  ContextMenuEvent,
  SetOption,
} from './collection.models';
import { CollectionService } from './collection.service';
import { ToastService } from '@shared/services/toast.service';

export interface CollectionState {
  cards: CollectionCardDto[];
  stats: CollectionStatsDto | null;
  totalItems: number;
  totalPages: number;
  filters: CollectionFilters;
  availableSets: SetOption[];
  availableRarities: string[];
  availableYears: number[];
  loading: boolean;
  toggling: Set<string>;
  error: string | null;
  contextMenu: ContextMenuEvent | null;
}

const initialFilters: CollectionFilters = {
  search: '',
  setId: null,
  rarity: null,
  year: null,
  ownedOnly: false,
  favoritesOnly: false,
  sortBy: 'name_asc',
  page: 1,
  pageSize: 60,
};

const initialState: CollectionState = {
  cards: [],
  stats: null,
  totalItems: 0,
  totalPages: 0,
  filters: initialFilters,
  availableSets: [],
  availableRarities: [],
  availableYears: [],
  loading: false,
  toggling: new Set<string>(),
  error: null,
  contextMenu: null,
};

export const CollectionStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed(store => ({
    hasCards: computed(() => store.cards().length > 0),
    statsLabel: computed(() => {
      const s = store.stats();
      if (!s) return '';
      return `${s.ownedCards} / ${s.totalCards} · ${s.completionPercent}%`;
    }),
  })),

  withMethods((store, svc = inject(CollectionService), toast = inject(ToastService)) => {
    const loadCards = rxMethod<CollectionFilters>(
      pipe(
        debounceTime(300),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(filters =>
          svc.getCards(filters).pipe(
            tapResponse({
              next: result =>
                patchState(store, {
                  cards: result.items,
                  totalItems: result.totalItems,
                  totalPages: result.totalPages,
                  loading: false,
                }),
              error: () => {
                patchState(store, { loading: false, error: 'Erreur chargement cartes' });
                toast.error('Impossible de charger les cartes');
              },
            })
          )
        )
      )
    );

    const loadStats = rxMethod<void>(
      pipe(
        switchMap(() =>
          svc.getStats().pipe(
            tapResponse({
              next: stats => patchState(store, { stats }),
              error: () => { /* non-bloquant */ },
            })
          )
        )
      )
    );

    return {
      loadCards: () => loadCards(store.filters()),

      loadStats: () => loadStats(),

      updateFilters(partial: Partial<CollectionFilters>): void {
        const newFilters: CollectionFilters = {
          ...store.filters(),
          ...partial,
          page: partial.page ?? 1,
        };
        patchState(store, { filters: newFilters });
        loadCards(newFilters);
      },

      setPage(page: number): void {
        const newFilters = { ...store.filters(), page };
        patchState(store, { filters: newFilters });
        loadCards(newFilters);
      },

      toggleOwned(cardId: string): void {
        // Optimistic update
        const card = store.cards().find(c => c.id === cardId);
        if (!card) return;

        const newIsOwned = !card.isOwned;
        const updatedCards = store.cards().map(c =>
          c.id === cardId ? { ...c, isOwned: newIsOwned } : c
        );
        const newToggling = new Set(store.toggling());
        newToggling.add(cardId);
        patchState(store, { cards: updatedCards, toggling: newToggling });

        svc.toggleOwned(cardId).pipe(
          tapResponse({
            next: status => {
              const finalToggling = new Set(store.toggling());
              finalToggling.delete(cardId);
              patchState(store, {
                cards: store.cards().map(c =>
                  c.id === cardId
                    ? { ...c, isOwned: status.isOwned, isFavorite: status.isFavorite, isPriority: status.isPriority }
                    : c
                ),
                toggling: finalToggling,
              });
              loadStats();
            },
            error: () => {
              // Rollback
              const rollbackToggling = new Set(store.toggling());
              rollbackToggling.delete(cardId);
              patchState(store, {
                cards: store.cards().map(c =>
                  c.id === cardId ? { ...c, isOwned: card.isOwned } : c
                ),
                toggling: rollbackToggling,
              });
              toast.error('Impossible de mettre à jour la carte');
            },
          })
        ).subscribe();
      },

      setFavorite(cardId: string, isFavorite: boolean): void {
        const card = store.cards().find(c => c.id === cardId);
        if (!card) return;

        // Optimistic update
        patchState(store, {
          cards: store.cards().map(c =>
            c.id === cardId ? { ...c, isFavorite } : c
          ),
          contextMenu: null,
        });

        svc.setFavorite(cardId, isFavorite).pipe(
          tapResponse({
            next: status => {
              patchState(store, {
                cards: store.cards().map(c =>
                  c.id === cardId
                    ? { ...c, isOwned: status.isOwned, isFavorite: status.isFavorite, isPriority: status.isPriority }
                    : c
                ),
              });
              loadStats();
            },
            error: () => {
              // Rollback
              patchState(store, {
                cards: store.cards().map(c =>
                  c.id === cardId ? { ...c, isFavorite: card.isFavorite } : c
                ),
              });
              toast.error('Impossible de modifier les favoris');
            },
          })
        ).subscribe();
      },

      setPriority(cardId: string, isPriority: boolean): void {
        const card = store.cards().find(c => c.id === cardId);
        if (!card) return;

        // Optimistic update
        patchState(store, {
          cards: store.cards().map(c =>
            c.id === cardId ? { ...c, isPriority } : c
          ),
          contextMenu: null,
        });

        svc.setPriority(cardId, isPriority).pipe(
          tapResponse({
            next: status => {
              patchState(store, {
                cards: store.cards().map(c =>
                  c.id === cardId
                    ? { ...c, isOwned: status.isOwned, isFavorite: status.isFavorite, isPriority: status.isPriority }
                    : c
                ),
              });
            },
            error: (err: unknown) => {
              // Rollback
              patchState(store, {
                cards: store.cards().map(c =>
                  c.id === cardId ? { ...c, isPriority: card.isPriority } : c
                ),
              });
              const msg = err instanceof Error ? err.message : 'Impossible de modifier la priorité';
              toast.error(msg);
            },
          })
        ).subscribe();
      },

      openContextMenu(event: ContextMenuEvent): void {
        patchState(store, { contextMenu: event });
      },

      closeContextMenu(): void {
        patchState(store, { contextMenu: null });
      },
    };
  })
);
