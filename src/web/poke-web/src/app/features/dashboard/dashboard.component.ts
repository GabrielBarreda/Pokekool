import { Component, inject } from '@angular/core';
import { AuthStore } from '@core/auth/auth.store';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { CardComponent } from '@shared/ui/card/card.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, CardComponent],
  template: `
    <app-page-header
      title="Tableau de bord"
      [subtitle]="'Bienvenue, ' + authStore.userDisplayName()"
    />

    <div class="dashboard-grid">
      <app-card title="Réservations du jour">
        <p class="placeholder-text">Chargement des données…</p>
      </app-card>
      <app-card title="Commandes en cours">
        <p class="placeholder-text">Chargement des données…</p>
      </app-card>
      <app-card title="Tables occupées">
        <p class="placeholder-text">Chargement des données…</p>
      </app-card>
    </div>
  `,
  styles: [`
    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: var(--space-4);
    }
    .placeholder-text {
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
    }
  `],
})
export class DashboardComponent {
  readonly authStore = inject(AuthStore);
}
