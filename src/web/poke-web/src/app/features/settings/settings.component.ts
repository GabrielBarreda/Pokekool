import {
  ChangeDetectionStrategy,
  Component,
  signal,
} from '@angular/core';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { AppearanceSettingsComponent } from './appearance/appearance-settings.component';
import { ProviderSettingsComponent } from './providers/provider-settings.component';
import { ProviderFreeSettingsComponent } from './providers/provider-free-settings.component';
import { StorageSettingsComponent } from './storage/storage-settings.component';

type Section = 'appearance' | 'providers-free' | 'providers' | 'storage';

interface NavItem {
  id: Section;
  label: string;
  icon: string;
}

const NAV_ITEMS: NavItem[] = [
  { id: 'appearance',     label: 'Apparence',       icon: '🎨' },
  { id: 'providers-free', label: 'Providers Free',  icon: '🆓' },
  { id: 'providers',      label: 'Providers',        icon: '🔌' },
  { id: 'storage',        label: 'Stockage',         icon: '🗃️' },
];

@Component({
  selector: 'app-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageHeaderComponent,
    AppearanceSettingsComponent,
    ProviderSettingsComponent,
    ProviderFreeSettingsComponent,
    StorageSettingsComponent,
  ],
  template: `
    <div class="settings-page" [class.settings-page--wide]="activeSection() === 'providers-free'">
      <app-page-header
        title="Paramètres"
        subtitle="Personnalisation et configuration des intégrations"
      />

      <div class="settings-layout">
        <!-- Sidebar -->
        <nav class="settings-nav" aria-label="Sections des paramètres">
          @for (item of navItems; track item.id) {
            <button
              class="settings-nav__item"
              [class.settings-nav__item--active]="activeSection() === item.id"
              (click)="setSection(item.id)"
              type="button"
            >
              <span class="settings-nav__icon">{{ item.icon }}</span>
              <span class="settings-nav__label">{{ item.label }}</span>
            </button>
          }
        </nav>

        <!-- Content -->
        <div class="settings-content">
          @if (activeSection() === 'appearance') {
            <app-appearance-settings />
          }
          @if (activeSection() === 'providers-free') {
            <app-provider-free-settings />
          }
          @if (activeSection() === 'providers') {
            <app-provider-settings />
          }
          @if (activeSection() === 'storage') {
            <app-storage-settings />
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .settings-page {
      padding: var(--space-6);
      max-width: 1000px;
      transition: max-width var(--duration-fast) var(--easing-standard);

      /* On élargit uniquement pour l'onglet 2-colonnes */
      &--wide { max-width: 1200px; }
    }

    .settings-layout {
      display: flex;
      gap: var(--space-6);
      align-items: flex-start;
    }

    .settings-nav {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
      width: 200px;
      flex-shrink: 0;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-xl);
      padding: var(--space-2);
    }

    .settings-nav__item {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      border-radius: var(--radius-lg);
      border: none;
      background: transparent;
      cursor: pointer;
      font-family: var(--font-family-sans);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--color-text-secondary);
      text-align: left;
      width: 100%;
      transition: background var(--duration-fast) var(--easing-standard),
                  color var(--duration-fast) var(--easing-standard);
    }
    .settings-nav__item:hover {
      background: var(--color-surface-raised);
      color: var(--color-text-primary);
    }
    .settings-nav__item--active {
      background: var(--color-brand-primary-subtle);
      color: var(--color-brand-primary);
    }

    .settings-nav__icon { font-size: var(--font-size-md); }
    .settings-nav__label { flex: 1; }

    .settings-content { flex: 1; min-width: 0; }
  `],
})
export class SettingsComponent {
  readonly navItems      = NAV_ITEMS;
  readonly activeSection = signal<Section>('appearance');

  setSection(section: Section): void {
    this.activeSection.set(section);
  }
}
