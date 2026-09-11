import { inject } from '@angular/core';
import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { pipe, switchMap, tap } from 'rxjs';
import { DashboardSummaryDto, SyncScheduleDto, EmbeddingReindexStatusDto } from './dashboard.models';
import { DashboardService } from './dashboard.service';
import { ToastService } from '@shared/services/toast.service';

export interface DashboardState {
  summary:          DashboardSummaryDto | null;
  syncStatus:       SyncScheduleDto | null;
  embeddingStatus:  EmbeddingReindexStatusDto | null;
  loading:          boolean;
  error:            string | null;
  lastRefreshedAt:  Date | null;
}

const initialState: DashboardState = {
  summary:          null,
  syncStatus:       null,
  embeddingStatus:  null,
  loading:          false,
  error:            null,
  lastRefreshedAt:  null,
};

export const DashboardStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withMethods((store, svc = inject(DashboardService), toast = inject(ToastService)) => {
    const loadSummary = rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() =>
          svc.getSummary().pipe(
            tapResponse({
              next: (summary: DashboardSummaryDto) =>
                patchState(store, {
                  summary,
                  loading:         false,
                  lastRefreshedAt: new Date(),
                }),
              error: () => {
                patchState(store, { loading: false, error: 'Erreur de chargement du tableau de bord' });
                toast.error('Impossible de charger le tableau de bord');
              },
            })
          )
        )
      )
    );

    const loadSyncStatus = rxMethod<void>(
      pipe(
        switchMap(() =>
          svc.getSyncStatus().pipe(
            tapResponse({
              next: (syncStatus: SyncScheduleDto) => patchState(store, { syncStatus }),
              error: () => { /* silencieux — pas critique */ },
            })
          )
        )
      )
    );

    // Réindexation embeddings DINOv2 : se déclenche automatiquement au démarrage de l'API
    // (EmbeddingReindexHostedService), sans action utilisateur — l'overlay doit donc la
    // découvrir par polling, contrairement au sync prix/pop qui part d'une action explicite.
    const loadEmbeddingStatus = rxMethod<void>(
      pipe(
        switchMap(() =>
          svc.getEmbeddingReindexStatus().pipe(
            tapResponse({
              next: (embeddingStatus: EmbeddingReindexStatusDto) => patchState(store, { embeddingStatus }),
              error: () => { /* silencieux — pas critique */ },
            })
          )
        )
      )
    );

    return {
      loadSummary,
      loadSyncStatus,
      loadEmbeddingStatus,
      refresh: () => {
        loadSummary();
        loadSyncStatus();
        loadEmbeddingStatus();
      },
    };
  })
);
