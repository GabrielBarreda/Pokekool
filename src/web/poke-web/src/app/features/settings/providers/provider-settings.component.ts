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
      <div class="providers-list">
        @for (config of providers; track config.provider) {
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

    .providers-list {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
    }
  `],
})
export class ProviderSettingsComponent implements OnInit {
  readonly store    = inject(SettingsStore);
  readonly providers = PROVIDERS;

  ngOnInit(): void {
    this.store.loadAllProviders();
  }
}
