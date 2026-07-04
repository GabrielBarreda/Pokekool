import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ThemeService, ThemeMode, FontFamily } from '@core/services/theme.service';
import { ThemeStore, ThemeUiMode } from '@core/theme/theme.store';
import { CardComponent } from '@shared/ui/card/card.component';

interface ThemeOption {
  value: ThemeMode;
  label: string;
  icon: string;
}

interface FontOption {
  value: FontFamily;
  label: string;
  preview: string;
}

const THEME_OPTIONS: ThemeOption[] = [
  { value: 'light',  label: 'Clair',   icon: '☀️' },
  { value: 'dark',   label: 'Sombre',  icon: '🌙' },
  { value: 'system', label: 'Système', icon: '💻' },
];

const FONT_OPTIONS: FontOption[] = [
  { value: 'Inter',     label: 'Inter',     preview: 'Aa' },
  { value: 'Roboto',    label: 'Roboto',    preview: 'Aa' },
  { value: 'System UI', label: 'System UI', preview: 'Aa' },
];

interface UiThemeOption {
  value: ThemeUiMode;
  label: string;
  icon: string;
  description: string;
}

const UI_THEME_OPTIONS: UiThemeOption[] = [
  { value: 'default', label: 'Défaut',  icon: '🗂️', description: 'Interface standard de l\'application.' },
  { value: 'pokemon', label: 'Pokémon', icon: '🃏', description: 'Classeur à pochettes, transitions dédiées (page Bourse).' },
];

@Component({
  selector: 'app-appearance-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  template: `
    <app-card title="Thème">
      <div class="theme-grid">
        @for (opt of themeOptions; track opt.value) {
          <button
            class="theme-option"
            [class.theme-option--active]="themeService.theme() === opt.value"
            (click)="setTheme(opt.value)"
            type="button"
          >
            <span class="theme-option__icon">{{ opt.icon }}</span>
            <span class="theme-option__label">{{ opt.label }}</span>
          </button>
        }
      </div>
    </app-card>

    <app-card title="Police d'écriture" style="margin-top: var(--space-4)">
      <div class="font-grid">
        @for (opt of fontOptions; track opt.value) {
          <button
            class="font-option"
            [class.font-option--active]="themeService.font() === opt.value"
            [style.font-family]="opt.value === 'System UI' ? '-apple-system, BlinkMacSystemFont, sans-serif' : opt.value"
            (click)="setFont(opt.value)"
            type="button"
          >
            <span class="font-option__preview">{{ opt.preview }}</span>
            <span class="font-option__label">{{ opt.label }}</span>
          </button>
        }
      </div>
    </app-card>

    <app-card title="Thème Pokémon" style="margin-top: var(--space-4)">
      <p class="ui-theme-hint">
        Habille la page Bourse en classeur à pochettes, avec ses propres transitions.
      </p>
      <div class="theme-grid">
        @for (opt of uiThemeOptions; track opt.value) {
          <button
            class="theme-option"
            [class.theme-option--active]="themeStore.themeMode() === opt.value"
            (click)="setUiTheme(opt.value)"
            type="button"
            [title]="opt.description"
          >
            <span class="theme-option__icon">{{ opt.icon }}</span>
            <span class="theme-option__label">{{ opt.label }}</span>
          </button>
        }
      </div>
    </app-card>
  `,
  styles: [`
    .theme-grid {
      display: flex;
      gap: var(--space-3);
    }

    .theme-option {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-4) var(--space-6);
      border: 2px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      background: var(--color-surface-raised);
      cursor: pointer;
      transition: border-color var(--duration-fast) var(--easing-standard),
                  background var(--duration-fast) var(--easing-standard);
      font-family: var(--font-family-sans);
      min-width: 100px;
    }
    .theme-option:hover {
      border-color: var(--color-brand-primary);
      background: var(--color-surface-base);
    }
    .theme-option--active {
      border-color: var(--color-brand-primary);
      background: var(--color-brand-primary-subtle);
    }
    .theme-option__icon { font-size: var(--font-size-2xl); }
    .theme-option__label {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-primary);
    }

    .font-grid {
      display: flex;
      gap: var(--space-3);
    }

    .font-option {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-4) var(--space-6);
      border: 2px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      background: var(--color-surface-raised);
      cursor: pointer;
      transition: border-color var(--duration-fast) var(--easing-standard),
                  background var(--duration-fast) var(--easing-standard);
      min-width: 110px;
    }
    .font-option:hover {
      border-color: var(--color-brand-primary);
      background: var(--color-surface-base);
    }
    .font-option--active {
      border-color: var(--color-brand-primary);
      background: var(--color-brand-primary-subtle);
    }
    .font-option__preview {
      font-size: var(--font-size-2xl);
      font-weight: var(--font-weight-bold);
      color: var(--color-text-primary);
    }
    .font-option__label {
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-secondary);
      font-family: var(--font-family-sans);
    }

    .ui-theme-hint {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      margin-bottom: var(--space-3);
    }
  `],
})
export class AppearanceSettingsComponent {
  readonly themeService = inject(ThemeService);
  readonly themeStore = inject(ThemeStore);
  readonly themeOptions = THEME_OPTIONS;
  readonly fontOptions  = FONT_OPTIONS;
  readonly uiThemeOptions = UI_THEME_OPTIONS;

  setTheme(theme: ThemeMode): void {
    this.themeService.setTheme(theme);
  }

  setFont(font: FontFamily): void {
    this.themeService.setFont(font);
  }

  setUiTheme(mode: ThemeUiMode): void {
    this.themeStore.setMode(mode);
  }
}
