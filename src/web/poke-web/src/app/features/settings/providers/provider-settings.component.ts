import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { PROVIDERS } from '../settings.models';
import { SettingsStore } from '../settings.store';
import { ProviderCardComponent } from './provider-card.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';

@Component({
  selector: 'app-provider-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ProviderCardComponent,
    SpinnerComponent,
  ],
  template: `
    @if (store.loadingProviders()) {
      <div class="loading-state">
        <app-spinner />
        <span>Chargement des providers…</span>
      </div>
    } @else {
      <div class="section-header">
        <span class="section-badge">Clé requise</span>
        <h2 class="section-title">Fournisseurs API</h2>
      </div>
      <p class="section-desc">
        Ces providers nécessitent une clé d'API ou des identifiants.
        Certains proposent un fallback scraping gratuit en attendant.
      </p>

      <div class="providers-list">
        @for (config of keyProviders; track config.provider) {
          <app-provider-card [config]="config" />
        }
      </div>
    }
  `,
  styles: [`
    .loading-state {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
      padding: var(--space-6) 0;
    }

    .section-header {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      margin-bottom: var(--space-1);
    }

    .section-badge {
      display: inline-flex;
      align-items: center;
      padding: 2px 8px;
      border-radius: var(--radius-full);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      background: var(--color-semantic-warning-subtle, #fef9c3);
      color: var(--color-semantic-warning, #ca8a04);
      flex-shrink: 0;
    }

    .section-title {
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
      margin: 0;
    }

    .section-desc {
      font-size: var(--font-size-sm);
      color: var(--color-text-muted);
      margin: 0 0 var(--space-4);
    }

    .providers-list {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
    }
  `],
})
export class ProviderSettingsComponent implements OnInit {
  readonly store = inject(SettingsStore);

  readonly keyProviders = PROVIDERS.filter(p => !p.noKeys);

  ngOnInit(): void {
    this.store.loadAllProviders();
  }
}
