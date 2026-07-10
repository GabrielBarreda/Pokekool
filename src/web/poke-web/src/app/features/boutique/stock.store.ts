import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { pipe, switchMap, tap } from 'rxjs';
import { ShopStockDto, ShopStockRequest, StockFilters } from './boutique.models';
import { BoutiqueService } from './boutique.service';
import { ToastService } from '@shared/services/toast.service';

interface StockState {
  items: ShopStockDto[];
  totalItems: number;
  filters: StockFilters;
  loading: boolean;
  saving: boolean;
}

const initialState: StockState = {
  items: [],
  totalItems: 0,
  filters: { type: null, search: null, page: 1, pageSize: 50 },
  loading: false,
  saving: false,
};

export const StockStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, service = inject(BoutiqueService), toast = inject(ToastService)) => {
    const reload = () => {
      patchState(store, { loading: true });
      service.getStock(store.filters()).subscribe({
        next: r => patchState(store, { items: r.items, totalItems: r.totalItems, loading: false }),
        error: () => { patchState(store, { loading: false }); toast.error('Impossible de charger le stock'); },
      });
    };
    return {
      load(): void { reload(); },
      setFilters(patch: Partial<StockFilters>): void {
        patchState(store, { filters: { ...store.filters(), ...patch, page: patch.page ?? 1 } });
        reload();
      },
      create: rxMethod<{ body: ShopStockRequest; onSuccess?: () => void }>(
        pipe(
          tap(() => patchState(store, { saving: true })),
          switchMap(({ body, onSuccess }) =>
            service.createStock(body).pipe(
              tapResponse({
                next: () => { patchState(store, { saving: false }); toast.success('Entrée de stock ajoutée'); onSuccess?.(); reload(); },
                error: () => { patchState(store, { saving: false }); toast.error('Erreur lors de la création'); },
              }),
            ),
          ),
        ),
      ),
      update: rxMethod<{ id: string; body: ShopStockRequest; onSuccess?: () => void }>(
        pipe(
          tap(() => patchState(store, { saving: true })),
          switchMap(({ id, body, onSuccess }) =>
            service.updateStock(id, body).pipe(
              tapResponse({
                next: updated => {
                  patchState(store, { saving: false, items: store.items().map(s => s.id === id ? updated : s) });
                  toast.success('Stock mis à jour'); onSuccess?.();
                },
                error: () => { patchState(store, { saving: false }); toast.error('Erreur lors de la mise à jour'); },
              }),
            ),
          ),
        ),
      ),
      remove: rxMethod<{ id: string }>(
        pipe(
          switchMap(({ id }) =>
            service.deleteStock(id).pipe(
              tapResponse({
                next: () => { patchState(store, { items: store.items().filter(s => s.id !== id) }); toast.success('Entrée retirée'); },
                error: () => toast.error('Erreur lors de la suppression'),
              }),
            ),
          ),
        ),
      ),
    };
  }),
);
