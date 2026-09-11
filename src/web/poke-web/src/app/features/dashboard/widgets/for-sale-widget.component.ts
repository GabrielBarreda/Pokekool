import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { CardComponent } from '@shared/ui/card/card.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';
import { ForSaleCardDto } from '../dashboard.models';

@Component({
  selector: 'app-for-sale-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, BadgeComponent, DecimalPipe],
  template: `
    <app-card title="Cartes en vente">
      @if (cards.length === 0) {
        <div class="coming-soon">
          <span class="coming-soon__icon">🏷️</span>
          <p class="coming-soon__text">Aucune carte en vente</p>
          <p class="coming-soon__sub">Cette fonctionnalité sera disponible prochainement.</p>
        </div>
      } @else {
        <table class="sale-table">
          <thead>
            <tr>
              <th></th>
              <th>Carte</th>
              <th>Prix</th>
              <th>Intéressés</th>
              <th>Depuis</th>
            </tr>
          </thead>
          <tbody>
            @for (card of cards; track card.cardId) {
              <tr>
                <td>
                  @if (card.imageSmall) {
                    <img [src]="card.imageSmall" [alt]="card.displayName || card.name" class="sale-table__thumb" />
                  }
                </td>
                <td class="sale-table__name">{{ card.displayName || card.name }}</td>
                <td class="sale-table__price">{{ card.salePrice | number:'1.2-2' }} {{ card.currency }}</td>
                <td>
                  <app-badge [variant]="card.interestedCount > 0 ? 'brand' : 'neutral'">
                    {{ card.interestedCount }}
                  </app-badge>
                </td>
                <td class="sale-table__time">{{ card.forSaleSince }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </app-card>
  `,
  styles: [`
    .coming-soon {
      display: flex; flex-direction: column; align-items: center;
      gap: var(--space-2); padding: var(--space-8) var(--space-4); text-align: center;
    }
    .coming-soon__icon { font-size: 32px; opacity: 0.3; }
    .coming-soon__text { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-secondary); margin: 0; }
    .coming-soon__sub  { font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0; }
    .sale-table { width: 100%; border-collapse: collapse; font-size: var(--font-size-sm); }
    .sale-table th { text-align: left; padding: var(--space-1) var(--space-2); color: var(--color-text-muted); font-weight: var(--font-weight-medium); border-bottom: 1px solid var(--color-border-subtle); }
    .sale-table td { padding: var(--space-2); border-bottom: 1px solid var(--color-border-subtle); }
    .sale-table__thumb { width: 32px; height: 44px; object-fit: contain; border-radius: var(--radius-sm); }
    .sale-table__name { font-weight: var(--font-weight-medium); color: var(--color-text-primary); }
    .sale-table__price { font-weight: var(--font-weight-semibold); color: var(--color-brand-primary); white-space: nowrap; }
    .sale-table__time { color: var(--color-text-muted); font-size: var(--font-size-xs); }
  `],
})
export class ForSaleWidgetComponent {
  @Input() cards: ForSaleCardDto[] = [];
}
