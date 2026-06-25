import { inject } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { computed } from '@angular/core';
import { pipe, switchMap, tap, debounceTime, distinctUntilChanged } from 'rxjs';
import { UserSummaryDto, AvailableRole, UsersFilters, DEFAULT_FILTERS } from './users.models';
import { UsersService } from './users.service';
import { RolesService } from '../roles/roles.service';
import { ToastService } from '@shared/services/toast.service';

export interface UsersState {
  users: UserSummaryDto[];
  totalItems: number;
  totalPages: number;
  filters: UsersFilters;
  availableRoles: AvailableRole[];
  loading: boolean;
  selectedUser: UserSummaryDto | null;
  error: string | null;
}

const initialState: UsersState = {
  users: [],
  totalItems: 0,
  totalPages: 0,
  filters: { ...DEFAULT_FILTERS },
  availableRoles: [],
  loading: false,
  selectedUser: null,
  error: null,
};

export const UsersStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed(store => ({
    activeUsers:   computed(() => store.users().filter(u => u.isActive)),
    inactiveUsers: computed(() => store.users().filter(u => !u.isActive)),
    hasResults:    computed(() => store.users().length > 0),
  })),

  withMethods((
    store,
    svc       = inject(UsersService),
    rolesSvc  = inject(RolesService),
    toast     = inject(ToastService),
  ) => ({

    /** Charge la page courante avec les filtres courants */
    loadUsers: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() =>
          svc.getUsers(store.filters()).pipe(
            tapResponse({
              next: result => patchState(store, {
                users:      result.items,
                totalItems: result.totalItems,
                totalPages: result.totalPages,
                loading:    false,
              }),
              error: () => {
                patchState(store, { loading: false, error: 'Erreur de chargement' });
                toast.error('Impossible de charger les utilisateurs');
              },
            })
          )
        )
      )
    ),

    /** Charge la liste des rôles disponibles pour les filtres + l'assignation */
    loadRoles: rxMethod<void>(
      pipe(
        switchMap(() =>
          rolesSvc.listRoles().pipe(
            tapResponse({
              next: roles => patchState(store, {
                availableRoles: roles.map(r => ({ code: r.code, name: r.name })),
              }),
              error: () => {},
            })
          )
        )
      )
    ),

    /** Met à jour un ou plusieurs filtres et recharge (retour à la page 1) */
    updateFilters(partial: Partial<UsersFilters>): void {
      patchState(store, {
        filters: { ...store.filters(), ...partial, page: partial.page ?? 1 },
      });
    },

    selectUser(user: UserSummaryDto): void {
      patchState(store, { selectedUser: user });
    },

    closeDetail(): void {
      patchState(store, { selectedUser: null });
    },

    activateUser: rxMethod<string>(
      pipe(
        switchMap(id =>
          svc.activateUser(id).pipe(
            tapResponse({
              next: () => {
                patchState(store, {
                  users: store.users().map(u =>
                    u.id === id ? { ...u, isActive: true } : u
                  ),
                  selectedUser: store.selectedUser()?.id === id
                    ? { ...store.selectedUser()!, isActive: true }
                    : store.selectedUser(),
                });
                toast.success('Utilisateur activé');
              },
              error: () => toast.error("Impossible d'activer l'utilisateur"),
            })
          )
        )
      )
    ),

    deactivateUser: rxMethod<string>(
      pipe(
        switchMap(id =>
          svc.deactivateUser(id).pipe(
            tapResponse({
              next: () => {
                patchState(store, {
                  users: store.users().map(u =>
                    u.id === id ? { ...u, isActive: false } : u
                  ),
                  selectedUser: store.selectedUser()?.id === id
                    ? { ...store.selectedUser()!, isActive: false }
                    : store.selectedUser(),
                });
                toast.success('Utilisateur désactivé');
              },
              error: () => toast.error("Impossible de désactiver l'utilisateur"),
            })
          )
        )
      )
    ),

    assignRoles: rxMethod<{ id: string; roleCodes: string[] }>(
      pipe(
        switchMap(({ id, roleCodes }) =>
          svc.assignRoles(id, roleCodes).pipe(
            tapResponse({
              next: updated => {
                patchState(store, {
                  users: store.users().map(u =>
                    u.id === id ? { ...u, roles: updated.roleCodes } : u
                  ),
                  selectedUser: store.selectedUser()?.id === id
                    ? { ...store.selectedUser()!, roles: updated.roleCodes }
                    : store.selectedUser(),
                });
                toast.success('Rôles mis à jour');
              },
              error: () => toast.error("Impossible de modifier les rôles"),
            })
          )
        )
      )
    ),

    deleteUser: rxMethod<string>(
      pipe(
        switchMap(id =>
          svc.deleteUser(id).pipe(
            tapResponse({
              next: () => {
                patchState(store, {
                  users:        store.users().filter(u => u.id !== id),
                  totalItems:   store.totalItems() - 1,
                  selectedUser: store.selectedUser()?.id === id ? null : store.selectedUser(),
                });
                toast.success('Utilisateur supprimé');
              },
              error: () => toast.error("Impossible de supprimer l'utilisateur"),
            })
          )
        )
      )
    ),
  }))
);
