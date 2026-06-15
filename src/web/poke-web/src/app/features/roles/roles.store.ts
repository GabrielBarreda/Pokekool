import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { inject, computed } from '@angular/core';
import { pipe, switchMap, tap, forkJoin } from 'rxjs';
import { RoleDto, PermissionDto, CreateRoleRequest, UpdateRoleRequest, groupPermissions, PermissionGroup } from './roles.models';
import { RolesService } from './roles.service';
import { ToastService } from '@shared/services/toast.service';

export interface RolesState {
  roles: RoleDto[];
  permissions: PermissionDto[];
  loading: boolean;
  saving: boolean;
  error: string | null;
}

const initialState: RolesState = {
  roles: [],
  permissions: [],
  loading: false,
  saving: false,
  error: null,
};

export const RolesStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed(store => ({
    customRoles: computed(() => store.roles().filter(r => !r.isSystem)),
    systemRoles: computed(() => store.roles().filter(r => r.isSystem)),
    permissionGroups: computed<PermissionGroup[]>(() => groupPermissions(store.permissions())),
    rolesForSelect: computed(() =>
      store.roles().map(r => ({ id: r.id, label: r.name, isSystem: r.isSystem }))
    ),
  })),

  withMethods((store, svc = inject(RolesService), toast = inject(ToastService)) => ({

    /** Charge rôles + catalogue permissions en parallèle */
    load: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() =>
          forkJoin({
            roles: svc.listRoles(),
            permissions: svc.listPermissions(),
          }).pipe(
            tapResponse({
              next: ({ roles, permissions }) =>
                patchState(store, { roles, permissions, loading: false }),
              error: () => {
                patchState(store, { loading: false, error: 'Erreur de chargement' });
                toast.error('Impossible de charger les rôles');
              },
            })
          )
        )
      )
    ),

    /** Charge seulement les rôles (si permissions déjà chargées) */
    loadRolesOnly: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true })),
        switchMap(() =>
          svc.listRoles().pipe(
            tapResponse({
              next: roles => patchState(store, { roles, loading: false }),
              error: () => patchState(store, { loading: false }),
            })
          )
        )
      )
    ),

    create: rxMethod<{ body: CreateRoleRequest; onSuccess?: () => void }>(
      pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(({ body, onSuccess }) =>
          svc.createRole(body).pipe(
            tapResponse({
              next: created => {
                patchState(store, {
                  roles: [...store.roles(), created],
                  saving: false,
                });
                toast.success(`Rôle « ${created.name} » créé`);
                onSuccess?.();
              },
              error: () => {
                patchState(store, { saving: false });
                toast.error('Erreur lors de la création du rôle');
              },
            })
          )
        )
      )
    ),

    update: rxMethod<{ id: string; body: UpdateRoleRequest; onSuccess?: () => void }>(
      pipe(
        tap(() => patchState(store, { saving: true, error: null })),
        switchMap(({ id, body, onSuccess }) =>
          svc.updateRole(id, body).pipe(
            tapResponse({
              next: updated => {
                patchState(store, {
                  roles: store.roles().map(r => (r.id === id ? updated : r)),
                  saving: false,
                });
                toast.success(`Rôle « ${updated.name} » mis à jour`);
                onSuccess?.();
              },
              error: () => {
                patchState(store, { saving: false });
                toast.error('Erreur lors de la mise à jour');
              },
            })
          )
        )
      )
    ),

    remove: rxMethod<{ id: string; name: string }>(
      pipe(
        tap(() => patchState(store, { saving: true })),
        switchMap(({ id, name }) =>
          svc.deleteRole(id).pipe(
            tapResponse({
              next: () => {
                patchState(store, {
                  roles: store.roles().filter(r => r.id !== id),
                  saving: false,
                });
                toast.success(`Rôle « ${name} » supprimé`);
              },
              error: () => {
                patchState(store, { saving: false });
                toast.error('Impossible de supprimer ce rôle');
              },
            })
          )
        )
      )
    ),
  }))
);
