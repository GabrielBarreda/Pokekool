import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { pipe, switchMap, tap } from 'rxjs';
import {
  ShopProductDto, ShopProductRequest, ProductFilters,
} from './boutique.models';
import { BoutiqueService } from './boutique.service';
import { ToastService } from '@shared/services/toast.service';

interface ProductsState {
  items: ShopProductDto[];
  totalItems: number;
  filters: ProductFilters;
  loading: boolean;
  saving: boolean;
}

const initialState: ProductsState = {
  items: [],
  totalItems: 0,
  filters: { type: null, isAvailable: null, search: null, page: 1, pageSize: 50 },
  loading: false,
  saving: false,
};

export const ProductsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, service = inject(BoutiqueService), toast = inject(ToastService)) => {
    const reload = () => {
      patchState(store, { loading: true });
      service.getProducts(store.filters()).subscribe({
        next: r => patchState(store, { items: r.items, totalItems: r.totalItems, loading: false }),
        error: () => { patchState(store, { loading: false }); toast.error('Impossible de charger le présentoir'); },
      });
    };
    return {
      load(): void { reload(); },
      setFilters(patch: Partial<ProductFilters>): void {
        patchState(store, { filters: { ...store.filters(), ...patch, page: patch.page ?? 1 } });
        reload();
      },
      create: rxMethod<{ body: ShopProductRequest; onSuccess?: () => void }>(
        pipe(
          tap(() => patchState(store, { saving: true })),
          switchMap(({ body, onSuccess }) =>
            service.createProduct(body).pipe(
              tapResponse({
                next: () => { patchState(store, { saving: false }); toast.success('Article ajouté'); onSuccess?.(); reload(); },
                error: () => { patchState(store, { saving: false }); toast.error('Erreur lors de la création'); },
              }),
            ),
          ),
        ),
      ),
      update: rxMethod<{ id: string; body: ShopProductRequest; onSuccess?: () => void }>(
        pipe(
          tap(() => patchState(store, { saving: true })),
          switchMap(({ id, body, onSuccess }) =>
            service.updateProduct(id, body).pipe(
              tapResponse({
                next: updated => {
                  patchState(store, { saving: false, items: store.items().map(p => p.id === id ? updated : p) });
                  toast.success('Article mis à jour'); onSuccess?.();
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
            service.deleteProduct(id).pipe(
              tapResponse({
                next: () => { patchState(store, { items: store.items().filter(p => p.id !== id) }); toast.success('Article supprimé'); },
                error: () => toast.error('Erreur lors de la suppression'),
              }),
            ),
          ),
        ),
      ),
      toggle: rxMethod<{ id: string; isAvailable: boolean }>(
        pipe(
          switchMap(({ id, isAvailable }) =>
            service.toggleAvailability(id, isAvailable).pipe(
              tapResponse({
                next: updated => patchState(store, { items: store.items().map(p => p.id === id ? updated : p) }),
                error: () => toast.error('Erreur lors du changement de disponibilité'),
              }),
            ),
          ),
        ),
      ),
    };
  }),
);
