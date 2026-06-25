import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { PROVIDERS } from '../settings.models';
import { SettingsStore } from '../settings.store';
import { ProviderCardComponent } from './provider-card.component';
import { SyncPanelComponent } from './sync-panel.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';

@Component({
  selector: 'app-provider-free-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ProviderCardComponent,
    SyncPanelComponent,
    SpinnerComponent,
  ],
  template: `
    @if (store.loadingProviders()) {
      <div class="loading-state">
        <app-spinner />
        <span>Chargement des providers…</span>
      </div>
    } @else {
      <div class="free-layout">

        <!-- ── Colonne gauche : provider cards ── -->
        <div class="free-layout__cards">
          <div class="section-header">
            <span class="section-badge">Gratuit</span>
            <h2 class="section-title">Scraping public</h2>
          </div>
          <p class="section-desc">
            Ces sources ne nécessitent aucune clé API — elles sont actives automatiquement.
          </p>

          <div class="cards-list">
            @for (config of freeProviders; track config.provider) {
              <app-provider-card [config]="config" />
            }
          </div>
        </div>

        <!-- ── Colonne droite : panel planning ── -->
        <div class="free-layout__panel">
          <app-sync-panel />
        </div>

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

    /* ── Layout 2 colonnes ── */
    .free-layout {
      display: grid;
      grid-template-columns: 1fr 340px;
      gap: var(--space-6);
      align-items: start;
    }

    /* ── Colonne gauche ── */
    .free-layout__cards {
      display: flex;
      flex-direction: column;
      min-width: 0;
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
      background: var(--color-semantic-success-subtle, #dcfce7);
      color: var(--color-semantic-success, #16a34a);
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

    .cards-list {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
    }

    /* ── Colonne droite ── */
    .free-layout__panel {
      /* Sticky pour rester visible pendant le scroll de la liste */
      position: sticky;
      top: var(--space-4);
    }
  `],
})
export class ProviderFreeSettingsComponent implements OnInit {
  readonly store = inject(SettingsStore);

  readonly freeProviders = PROVIDERS.filter(p => p.noKeys);

  ngOnInit(): void {
    this.store.loadAllProviders();
  }
}
