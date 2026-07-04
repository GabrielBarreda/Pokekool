import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ThemeStore } from '@core/theme/theme.store';

/**
 * Toggle discret "thème Pokémon" — bascule ThemeStore.themeMode entre
 * 'default' et 'pokemon'. Conçu pour être posé dans un header de page
 * (ex: actions du page-header de la Bourse) sans dépendance au layout.
 */
@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      class="theme-toggle"
      type="button"
      role="switch"
      [attr.aria-checked]="themeStore.isPokemon()"
      [class.theme-toggle--on]="themeStore.isPokemon()"
      [title]="themeStore.isPokemon() ? 'Revenir au thème par défaut' : 'Activer le thème Pokémon'"
      (click)="themeStore.toggle()"
    >
      <span class="theme-toggle__ball" aria-hidden="true">
        <svg viewBox="0 0 24 24" width="16" height="16">
          <path d="M12 2a10 10 0 0 0-9.95 9H9a3 3 0 0 1 6 0h6.95A10 10 0 0 0 12 2z" fill="#EF4444"/>
          <path d="M12 22a10 10 0 0 0 9.95-9H15a3 3 0 0 1-6 0H2.05A10 10 0 0 0 12 22z" fill="#F8F9FA"/>
          <circle cx="12" cy="11" r="3.4" fill="#212529"/>
          <circle cx="12" cy="11" r="2" fill="#F8F9FA"/>
        </svg>
      </span>
      <span class="theme-toggle__label">{{ themeStore.isPokemon() ? 'Pokémon' : 'Défaut' }}</span>
    </button>
  `,
  styles: [`
    .theme-toggle {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      height: 36px;
      padding: 0 var(--space-3) 0 var(--space-2);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-full);
      background: var(--color-surface-raised);
      color: var(--color-text-secondary);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      cursor: pointer;
      transition: border-color var(--duration-fast) var(--easing-standard),
                  background var(--duration-fast) var(--easing-standard),
                  color var(--duration-fast) var(--easing-standard);

      &:hover { border-color: var(--color-brand-primary); color: var(--color-text-primary); }
      &--on {
        border-color: var(--color-brand-primary);
        background: var(--color-brand-primary-subtle);
        color: var(--color-brand-primary);
      }
    }
    .theme-toggle__ball {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 22px;
      height: 22px;
      flex-shrink: 0;
      transition: transform var(--duration-normal) var(--easing-standard);
    }
    .theme-toggle--on .theme-toggle__ball {
      transform: rotate(180deg);
    }
    .theme-toggle__label {
      white-space: nowrap;
    }

    @media (prefers-reduced-motion: reduce) {
      .theme-toggle__ball { transition: none; }
    }
  `],
})
export class ThemeToggleComponent {
  readonly themeStore = inject(ThemeStore);
}
