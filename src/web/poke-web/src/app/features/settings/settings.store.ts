import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { pipe, mergeMap, tap, forkJoin } from 'rxjs';
import { PROVIDERS, ProviderKeyDto, ProviderSettingsDto } from './settings.models';
import { SettingsService } from './settings.service';
import { ToastService } from '@shared/services/toast.service';

// ── Edit state per key ────────────────────────────────────────────────────────

export interface KeyEditState {
  isEditing: boolean;
  value: string;
  saving: boolean;
}

// ── Store state ───────────────────────────────────────────────────────────────

export interface SettingsState {
  /** Keys indexed by provider name */
  providerKeys:      Record<string, ProviderKeyDto[]>;
  /**
   * Edit state indexed by composite key `"<provider>:<keyName>"`.
   * Flat structure avoids nested Record patching complexity.
   */
  keyEditState:      Record<string, KeyEditState>;
  loadingProviders:  boolean;
  testingConnection: boolean;
  error:             string | null;
}

const initialState: SettingsState = {
  providerKeys:      {},
  keyEditState:      {},
  loadingProviders:  false,
  testingConnection: false,
  error:             null,
};

// ── Helper ────────────────────────────────────────────────────────────────────

function editKey(provider: string, key: string): string {
  return `${provider}:${key}`;
}

// ── Store ─────────────────────────────────────────────────────────────────────

export const SettingsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed(() => ({})),

  withMethods((store, svc = inject(SettingsService), toast = inject(ToastService)) => ({

    /** Load settings for all providers in parallel. */
    loadAllProviders: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loadingProviders: true, error: null })),
        mergeMap(() => {
          // Only providers that have actual keys to fetch
          const providerIds = PROVIDERS
            .filter(p => !p.noKeys)
            .map(p => p.provider);

          const requests = Object.fromEntries(
            providerIds.map(id => [id, svc.getSettings(id)])
          );

          return forkJoin(requests).pipe(
            tapResponse({
              next: (results: Record<string, ProviderSettingsDto>) => {
                const providerKeys: Record<string, ProviderKeyDto[]> = {};
                const keyEditState: Record<string, KeyEditState> = { ...store.keyEditState() };

                for (const [provider, dto] of Object.entries(results)) {
                  providerKeys[provider] = dto.keys;
                  for (const k of dto.keys) {
                    const ek = editKey(provider, k.key);
                    if (!keyEditState[ek]) {
                      keyEditState[ek] = { isEditing: false, value: '', saving: false };
                    }
                  }
                }

                patchState(store, {
                  providerKeys,
                  keyEditState,
                  loadingProviders: false,
                });
              },
              error: () => {
                patchState(store, { loadingProviders: false, error: 'Erreur chargement providers' });
                toast.error('Impossible de charger les credentials des providers');
              },
            })
          );
        })
      )
    ),

    // ── Edit lifecycle ──────────────────────────────────────────────────────

    startEdit(provider: string, key: string): void {
      const ek = editKey(provider, key);
      patchState(store, {
        keyEditState: {
          ...store.keyEditState(),
          [ek]: { isEditing: true, value: '', saving: false },
        },
      });
    },

    cancelEdit(provider: string, key: string): void {
      const ek = editKey(provider, key);
      patchState(store, {
        keyEditState: {
          ...store.keyEditState(),
          [ek]: { isEditing: false, value: '', saving: false },
        },
      });
    },

    updateEditValue(provider: string, key: string, value: string): void {
      const ek = editKey(provider, key);
      const current = store.keyEditState()[ek];
      if (!current) return;
      patchState(store, {
        keyEditState: { ...store.keyEditState(), [ek]: { ...current, value } },
      });
    },

    getEditState(provider: string, key: string): KeyEditState | undefined {
      return store.keyEditState()[editKey(provider, key)];
    },

    // ── Save ────────────────────────────────────────────────────────────────

    saveSetting: rxMethod<{ provider: string; key: string; value: string }>(
      pipe(
        tap(({ provider, key }) => {
          const ek = editKey(provider, key);
          const current = store.keyEditState()[ek];
          patchState(store, {
            keyEditState: { ...store.keyEditState(), [ek]: { ...current, saving: true } },
          });
        }),
        // mergeMap to allow concurrent saves for different keys
        mergeMap(({ provider, key, value }) =>
          svc.upsertSetting(provider, key, value).pipe(
            tapResponse({
              next: () => {
                const ek = editKey(provider, key);
                const updatedKeys = (store.providerKeys()[provider] ?? []).map(k =>
                  k.key === key ? { ...k, isConfigured: true } : k
                );
                patchState(store, {
                  providerKeys: { ...store.providerKeys(), [provider]: updatedKeys },
                  keyEditState: {
                    ...store.keyEditState(),
                    [ek]: { isEditing: false, value: '', saving: false },
                  },
                });
                toast.success('Credential enregistré');
              },
              error: () => {
                const ek = editKey(provider, key);
                const current = store.keyEditState()[ek];
                patchState(store, {
                  keyEditState: { ...store.keyEditState(), [ek]: { ...current, saving: false } },
                });
                toast.error('Erreur lors de l\'enregistrement');
              },
            })
          )
        )
      )
    ),

    // ── Save direct (sans flow edit/cancel) — pour selects et toggles ────────

    /**
     * Sauvegarde immédiate d'une clé sans passer par le flow isEditing/cancel.
     * Utilisé pour le select "Mode d'extraction" (extraction_mode).
     */
    saveKeyDirect: rxMethod<{ provider: string; key: string; value: string }>(
      pipe(
        mergeMap(({ provider, key, value }) =>
          svc.upsertSetting(provider, key, value).pipe(
            tapResponse({
              next: () => {
                const updatedKeys = (store.providerKeys()[provider] ?? []).map(k =>
                  k.key === key ? { ...k, isConfigured: true } : k
                );
                patchState(store, {
                  providerKeys: { ...store.providerKeys(), [provider]: updatedKeys },
                });
                toast.success(`${key} mis à jour`);
              },
              error: () => toast.error(`Erreur lors de la mise à jour de ${key}`),
            })
          )
        )
      )
    ),

    // ── Test connection (CardMarket only) ───────────────────────────────────

    testCardmarketConnection: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { testingConnection: true })),
        mergeMap(() =>
          svc.testConnection('cardmarket').pipe(
            tapResponse({
              next: result => {
                patchState(store, { testingConnection: false });
                if (result.success) {
                  toast.success(result.message || 'Connexion CardMarket réussie');
                } else {
                  toast.error(result.message || 'Connexion CardMarket échouée');
                }
              },
              error: () => {
                patchState(store, { testingConnection: false });
                toast.error('Erreur lors du test de connexion');
              },
            })
          )
        )
      )
    ),
  }))
);
