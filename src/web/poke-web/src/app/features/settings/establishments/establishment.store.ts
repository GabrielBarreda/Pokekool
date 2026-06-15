import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject, computed } from '@angular/core';
import { pipe, switchMap, tap } from 'rxjs';
import {
  EstablishmentDto,
  CreateEstablishmentRequest,
  UpdateEstablishmentRequest,
} from './establishment.models';
import { EstablishmentService } from './establishment.service';
import { ToastService } from '@shared/services/toast.service';

export interface EstablishmentState {
  establishments: EstablishmentDto[];
  selected: EstablishmentDto | null;
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: EstablishmentState = {
  establishments: [],
  selected: null,
  loading: false,
  saving: false,
  error: null,
};

export const EstablishmentStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed(store => ({
    activeEstablishments: computed(() =>
      store.establishments().filter(e => e.isActive)
    ),
    count: computed(() => store.establishments().length),
  })),

  withMethods((store, service = inject(EstablishmentService), toast = inject(ToastService)) => ({
    /** Load all establishments */
    load: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() =>
          service.list(true).pipe(
            tapResponse({
              next: establishments => patchState(store, { establishments, loading: false }),
              error: () => {
                patchState(store, { loading: false, error: 'Impossible de charger les établissements' });
                toast.error('Impossible de charger les établissements');
              },
            })
          )
        )
      )
    ),

    /** Create */
    create: rxMethod<{ body: CreateEstablishmentRequest; onSuccess?: () => void }>(
      pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(({ body, onSuccess }) =>
          service.create(body).pipe(
            tapResponse({
              next: created => {
                patchState(store, {
                  establishments: [...store.establishments(), created],
                  saving: false,
                });
                toast.success(`Établissement « ${created.name} » créé`);
                onSuccess?.();
              },
              error: () => {
                patchState(store, { saving: false, error: 'Erreur lors de la création' });
                toast.error('Erreur lors de la création');
              },
            })
          )
        )
      )
    ),

    /** Update */
    update: rxMethod<{ id: string; body: UpdateEstablishmentRequest; onSuccess?: () => void }>(
      pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(({ id, body, onSuccess }) =>
          service.update(id, body).pipe(
            tapResponse({
              next: updated => {
                patchState(store, {
                  establishments: store.establishments().map(e => (e.id === id ? updated : e)),
                  saving: false,
                });
                toast.success(`Établissement « ${updated.name} » mis à jour`);
                onSuccess?.();
              },
              error: () => {
                patchState(store, { saving: false, error: 'Erreur lors de la mise à jour' });
                toast.error('Erreur lors de la mise à jour');
              },
            })
          )
        )
      )
    ),

    /** Soft delete */
    remove: rxMethod<{ id: string; name: string }>(
      pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(({ id, name }) =>
          service.delete(id).pipe(
            tapResponse({
              next: () => {
                patchState(store, {
                  establishments: store.establishments().filter(e => e.id !== id),
                  saving: false,
                });
                toast.success(`Établissement « ${name} » supprimé`);
              },
              error: () => {
                patchState(store, { saving: false, error: 'Erreur lors de la suppression' });
                toast.error('Erreur lors de la suppression');
              },
            })
          )
        )
      )
    ),

    select(establishment: EstablishmentDto | null): void {
      patchState(store, { selected: establishment });
    },

    clearError(): void {
      patchState(store, { error: null });
    },
  }))
);
