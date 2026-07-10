import { inject, InjectionToken } from '@angular/core';
import { Router } from '@angular/router';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { computed } from '@angular/core';
import { tapResponse } from '@ngrx/operators';
import { pipe, switchMap, tap } from 'rxjs';
import { AuthService } from './auth.service';
import { AuthState, LoginRequest, RegisterRequest } from './auth.models';

// ── Storage keys ──────────────────────────────────────────────
const STORAGE_KEY = 'pokekool_auth';

function loadFromStorage(): Partial<AuthState> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return {};
    return JSON.parse(raw) as Partial<AuthState>;
  } catch {
    return {};
  }
}

function saveToStorage(state: AuthState): void {
  try {
    const { user, accessToken, refreshToken, accessTokenExpiresAt } = state;
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ user, accessToken, refreshToken, accessTokenExpiresAt }));
  } catch {
    // storage unavailable — continue without persistence
  }
}

function clearStorage(): void {
  localStorage.removeItem(STORAGE_KEY);
}

// ── Initial state ─────────────────────────────────────────────
const persisted = loadFromStorage();

const initialState: AuthState = {
  user: persisted.user ?? null,
  accessToken: persisted.accessToken ?? null,
  refreshToken: persisted.refreshToken ?? null,
  accessTokenExpiresAt: persisted.accessTokenExpiresAt ?? null,
  isAuthenticated: !!(persisted.accessToken && persisted.user),
  isLoading: false,
  error: null,
};

// ── Store ─────────────────────────────────────────────────────
export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState<AuthState>(initialState),

  withComputed(store => ({
    userDisplayName: computed(() => {
      const u = store.user();
      if (!u) return '';
      return `${u.firstName} ${u.lastName}`.trim() || u.email;
    }),
    userInitials: computed(() => {
      const u = store.user();
      if (!u) return '?';
      const f = u.firstName?.[0] ?? '';
      const l = u.lastName?.[0] ?? '';
      return (f + l).toUpperCase() || u.email[0].toUpperCase();
    }),
  })),

  withMethods((store, authService = inject(AuthService), router = inject(Router)) => ({

    // ── Login ────────────────────────────────────────────────
    login: rxMethod<LoginRequest>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap(req =>
          authService.login(req).pipe(
            tapResponse({
              next: res => {
                const next: Partial<AuthState> = {
                  user: res.user,
                  accessToken: res.accessToken,
                  refreshToken: res.refreshToken,
                  accessTokenExpiresAt: res.accessTokenExpiresAt,
                  isAuthenticated: true,
                  isLoading: false,
                  error: null,
                };
                patchState(store, next);
                saveToStorage({ ...store, ...next } as AuthState);
                const roles = res.user?.roles ?? [];
                router.navigate([roles.includes('admin') ? '/dashboard' : '/client/collection']);
              },
              error: (err: { error?: { title?: string } }) => {
                patchState(store, {
                  isLoading: false,
                  error: err?.error?.title ?? 'Identifiants incorrects',
                });
              },
            }),
          ),
        ),
      ),
    ),

    // ── Register (compte client) ─────────────────────────────
    register: rxMethod<RegisterRequest>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap(req =>
          authService.register(req).pipe(
            tapResponse({
              next: res => {
                const next: Partial<AuthState> = {
                  user: res.user,
                  accessToken: res.accessToken,
                  refreshToken: res.refreshToken,
                  accessTokenExpiresAt: res.accessTokenExpiresAt,
                  isAuthenticated: true,
                  isLoading: false,
                  error: null,
                };
                patchState(store, next);
                saveToStorage({ ...store, ...next } as AuthState);
                router.navigate(['/client/collection']);
              },
              error: (err: { error?: { title?: string; detail?: string } }) => {
                patchState(store, {
                  isLoading: false,
                  error: err?.error?.detail ?? err?.error?.title ?? 'Inscription impossible',
                });
              },
            }),
          ),
        ),
      ),
    ),

    // ── Logout ───────────────────────────────────────────────
    logout(): void {
      const rt = store.refreshToken();
      if (rt) {
        authService.logout(rt).subscribe({ error: () => {} });
      }
      patchState(store, {
        user: null,
        accessToken: null,
        refreshToken: null,
        accessTokenExpiresAt: null,
        isAuthenticated: false,
        error: null,
      });
      clearStorage();
      router.navigate(['/login']);
    },

    // ── Refresh token ────────────────────────────────────────
    refresh: rxMethod<void>(
      pipe(
        switchMap(() => {
          const rt = store.refreshToken();
          if (!rt) throw new Error('no refresh token');
          return authService.refresh(rt).pipe(
            tapResponse({
              next: res => {
                const next: Partial<AuthState> = {
                  accessToken: res.accessToken,
                  refreshToken: res.refreshToken,
                  accessTokenExpiresAt: res.accessTokenExpiresAt,
                  isAuthenticated: true,
                };
                patchState(store, next);
                saveToStorage({ ...store, ...next } as AuthState);
              },
              error: () => {
                patchState(store, { isAuthenticated: false, user: null, accessToken: null });
                clearStorage();
                router.navigate(['/login']);
              },
            }),
          );
        }),
      ),
    ),

    // ── Hydrate permissions from /me ─────────────────────────
    loadCurrentUser: rxMethod<void>(
      pipe(
        switchMap(() =>
          authService.me().pipe(
            tapResponse({
              next: me => {
                patchState(store, {
                  user: {
                    id: me.id,
                    email: me.email,
                    firstName: me.firstName,
                    lastName: me.lastName,
                    tenantId: me.tenantId,
                    roles: me.roles,
                  },
                  isAuthenticated: true,
                });
              },
              error: () => {},
            }),
          ),
        ),
      ),
    ),
  })),
);
