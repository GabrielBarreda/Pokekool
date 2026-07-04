import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { computed } from '@angular/core';

/**
 * Mode d'apparence "métier" — distinct de ThemeService (clair/sombre/police).
 * 'pokemon' active le classeur binder, les transitions dédiées, etc. sur les
 * pages qui le supportent (Bourse en premier lieu).
 */
export type ThemeUiMode = 'default' | 'pokemon';

export interface ThemeUiState {
  themeMode: ThemeUiMode;
}

const STORAGE_KEY = 'pkk_theme_mode';
const BODY_CLASS = 'theme-pokemon';

function readInitialMode(): ThemeUiMode {
  if (typeof localStorage === 'undefined') return 'default';
  try {
    return localStorage.getItem(STORAGE_KEY) === 'pokemon' ? 'pokemon' : 'default';
  } catch {
    return 'default';
  }
}

function applyBodyClass(mode: ThemeUiMode): void {
  if (typeof document === 'undefined') return;
  document.body.classList.toggle(BODY_CLASS, mode === 'pokemon');
}

const initialState: ThemeUiState = {
  themeMode: readInitialMode(),
};

export const ThemeStore = signalStore(
  { providedIn: 'root' },
  withState<ThemeUiState>(initialState),

  withComputed(store => ({
    isPokemon: computed(() => store.themeMode() === 'pokemon'),
  })),

  withMethods(store => {
    function setMode(mode: ThemeUiMode): void {
      patchState(store, { themeMode: mode });
      applyBodyClass(mode);
      if (typeof localStorage !== 'undefined') {
        try {
          localStorage.setItem(STORAGE_KEY, mode);
        } catch {
          // stockage indisponible (navigation privée…) — pas bloquant
        }
      }
    }

    return {
      setMode,

      toggle(): void {
        setMode(store.themeMode() === 'pokemon' ? 'default' : 'pokemon');
      },

      /** Appelé une fois au boot (APP_INITIALIZER) pour appliquer la classe
       *  body avant le premier paint et éviter un flash de thème par défaut. */
      init(): void {
        applyBodyClass(store.themeMode());
      },
    };
  }),
);
