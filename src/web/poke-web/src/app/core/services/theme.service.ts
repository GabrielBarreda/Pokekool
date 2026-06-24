import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';
export type FontFamily = 'Inter' | 'Roboto' | 'System UI';

const THEME_KEY = 'pkk_theme';
const FONT_KEY  = 'pkk_font';

const FONT_VALUES: Record<FontFamily, string> = {
  'Inter':     'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
  'Roboto':    'Roboto, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
  'System UI': '-apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
};

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<ThemeMode>('system');
  readonly font  = signal<FontFamily>('Inter');

  /** Called once at boot (APP_INITIALIZER) */
  init(): void {
    const savedTheme = localStorage.getItem(THEME_KEY) as ThemeMode | null;
    const savedFont  = localStorage.getItem(FONT_KEY)  as FontFamily | null;

    if (savedTheme) this.theme.set(savedTheme);
    if (savedFont)  this.font.set(savedFont);

    this._applyTheme(this.theme());
    this._applyFont(this.font());

    // React to OS preference changes when in 'system' mode
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (this.theme() === 'system') this._applyTheme('system');
    });
  }

  setTheme(theme: ThemeMode): void {
    this.theme.set(theme);
    localStorage.setItem(THEME_KEY, theme);
    this._applyTheme(theme);
  }

  setFont(font: FontFamily): void {
    this.font.set(font);
    localStorage.setItem(FONT_KEY, font);
    this._applyFont(font);
  }

  private _applyTheme(theme: ThemeMode): void {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const resolved = theme === 'system' ? (prefersDark ? 'dark' : 'light') : theme;
    document.documentElement.setAttribute('data-theme', resolved);
  }

  private _applyFont(font: FontFamily): void {
    document.documentElement.style.setProperty('--font-family-sans', FONT_VALUES[font]);
  }
}
