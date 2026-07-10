import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { pipe, switchMap, tap } from 'rxjs';
import {
  ShopAppointmentDto, AppointmentFilters, AppointmentStatus, UpdateAppointmentRequest,
} from './boutique.models';
import { BoutiqueService } from './boutique.service';
import { ToastService } from '@shared/services/toast.service';

interface AppointmentsState {
  items: ShopAppointmentDto[];
  filters: AppointmentFilters;
  loading: boolean;
  saving: boolean;
}

const initialState: AppointmentsState = {
  items: [],
  filters: { status: null, serviceType: null, from: null, to: null },
  loading: false,
  saving: false,
};

export const AppointmentsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, service = inject(BoutiqueService), toast = inject(ToastService)) => {
    const reload = () => {
      patchState(store, { loading: true });
      service.getAppointments(store.filters()).subscribe({
        next: items => patchState(store, { items, loading: false }),
        error: () => { patchState(store, { loading: false }); toast.error('Impossible de charger les rendez-vous'); },
      });
    };
    return {
      load(): void { reload(); },
      setFilters(patch: Partial<AppointmentFilters>): void {
        patchState(store, { filters: { ...store.filters(), ...patch } });
        reload();
      },
      changeStatus: rxMethod<{ id: string; status: AppointmentStatus }>(
        pipe(
          tap(() => patchState(store, { saving: true })),
          switchMap(({ id, status }) =>
            service.updateAppointmentStatus(id, status).pipe(
              tapResponse({
                next: updated => {
                  patchState(store, { saving: false, items: store.items().map(a => a.id === id ? updated : a) });
                  toast.success('Statut mis à jour');
                },
                error: () => { patchState(store, { saving: false }); toast.error('Erreur lors du changement de statut'); },
              }),
            ),
          ),
        ),
      ),
      updateSlot: rxMethod<{ id: string; body: UpdateAppointmentRequest; onSuccess?: () => void }>(
        pipe(
          tap(() => patchState(store, { saving: true })),
          switchMap(({ id, body, onSuccess }) =>
            service.updateAppointment(id, body).pipe(
              tapResponse({
                next: updated => {
                  patchState(store, { saving: false, items: store.items().map(a => a.id === id ? updated : a) });
                  toast.success('Rendez-vous mis à jour'); onSuccess?.();
                },
                error: () => { patchState(store, { saving: false }); toast.error('Erreur lors de la mise à jour'); },
              }),
            ),
          ),
        ),
      ),
    };
  }),
);
