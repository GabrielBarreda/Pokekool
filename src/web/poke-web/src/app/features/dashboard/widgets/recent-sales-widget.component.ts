import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CardComponent } from '@shared/ui/card/card.component';
import { RecentSaleDto } from '../dashboard.models';

@Component({
  selector: 'app-recent-sales-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  template: `
    <app-card title="Dernières ventes">
      <div class="coming-soon">
        <span class="coming-soon__icon">📊</span>
        <p class="coming-soon__text">Bientôt disponible</p>
        <p class="coming-soon__sub">L'historique des ventes sera disponible dans une prochaine mise à jour.</p>
      </div>
    </app-card>
  `,
  styles: [`
    .coming-soon {
      display: flex; flex-direction: column; align-items: center;
      gap: var(--space-2); padding: var(--space-8) var(--space-4); text-align: center;
      opacity: 0.6;
    }
    .coming-soon__icon { font-size: 32px; }
    .coming-soon__text { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-secondary); margin: 0; }
    .coming-soon__sub  { font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0; }
  `],
})
export class RecentSalesWidgetComponent {
  /** Réservé pour le branchement futur quand CardSale sera implémenté. */
  @Input() sales: RecentSaleDto[] = [];
}
