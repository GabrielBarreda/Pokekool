import { inject, computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { pipe, switchMap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';

interface PermissionState {
  permissions: string[];
  loaded: boolean;
}

export const PermissionStore = signalStore(
  { providedIn: 'root' },
  withState<PermissionState>({ permissions: [], loaded: false }),

  withComputed(store => ({
    hasPermission: computed(() => (permission: string) =>
      store.permissions().includes(permission),
    ),
    hasAnyPermission: computed(() => (...perms: string[]) =>
      perms.some(p => store.permissions().includes(p)),
    ),
  })),

  withMethods((store, http = inject(HttpClient)) => ({

    load: rxMethod<void>(
      pipe(
        switchMap(() =>
          http.get<{ id: string; email: string; permissions: string[] }>(`${environment.apiBaseUrl}/v1/me`).pipe(
            tapResponse({
              next: me => patchState(store, { permissions: me.permissions ?? [], loaded: true }),
              error: () => patchState(store, { loaded: true }),
            }),
          ),
        ),
      ),
    ),

    setPermissions(permissions: string[]): void {
      patchState(store, { permissions, loaded: true });
    },

    clear(): void {
      patchState(store, { permissions: [], loaded: false });
    },
  })),
);
