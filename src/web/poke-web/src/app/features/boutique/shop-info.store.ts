import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { pipe, switchMap, tap } from 'rxjs';
import { ShopDto, UpdateShopInfoRequest } from './boutique.models';
import { BoutiqueService } from './boutique.service';
import { ToastService } from '@shared/services/toast.service';

interface ShopInfoState {
  shop: ShopDto | null;
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: ShopInfoState = { shop: null, loading: false, saving: false, error: null };

export const ShopInfoStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, service = inject(BoutiqueService), toast = inject(ToastService)) => ({
    load: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() =>
          service.getInfo().pipe(
            tapResponse({
              next: shop => patchState(store, { shop, loading: false }),
              error: () => {
                patchState(store, { loading: false, error: 'Impossible de charger la boutique' });
                toast.error('Impossible de charger les informations de la boutique');
              },
            }),
          ),
        ),
      ),
    ),
    save: rxMethod<{ body: UpdateShopInfoRequest; onSuccess?: () => void }>(
      pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(({ body, onSuccess }) =>
          service.updateInfo(body).pipe(
            tapResponse({
              next: shop => {
                patchState(store, { shop, saving: false });
                toast.success('Informations de la boutique enregistrées');
                onSuccess?.();
              },
              error: () => {
                patchState(store, { saving: false, error: 'Erreur lors de l’enregistrement' });
                toast.error('Erreur lors de l’enregistrement');
              },
            }),
          ),
        ),
      ),
    ),
  })),
);
