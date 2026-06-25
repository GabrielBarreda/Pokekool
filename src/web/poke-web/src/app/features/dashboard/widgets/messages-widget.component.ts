import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CardComponent } from '@shared/ui/card/card.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';

@Component({
  selector: 'app-messages-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, BadgeComponent, RouterLink],
  template: `
    <app-card>
      <div class="kpi-widget">
        <span class="kpi-widget__icon">✉️</span>
        <div class="kpi-widget__body">
          <span class="kpi-widget__label">Messages</span>
          <div class="kpi-widget__value-row">
            <span class="kpi-widget__value">{{ pendingCount }}</span>
            @if (pendingCount > 0) {
              <app-badge variant="danger">{{ pendingCount }} en attente</app-badge>
            } @else {
              <span class="kpi-widget__sub">aucun en attente</span>
            }
          </div>
        </div>
      </div>
      <a routerLink="/messages" class="kpi-widget__link">Voir les messages →</a>
    </app-card>
  `,
  styles: [`
    .kpi-widget {
      display: flex; gap: var(--space-3); align-items: flex-start;
    }
    .kpi-widget__icon { font-size: 24px; line-height: 1; flex-shrink: 0; }
    .kpi-widget__body { display: flex; flex-direction: column; gap: var(--space-1); }
    .kpi-widget__label { font-size: var(--font-size-xs); color: var(--color-text-muted); font-weight: var(--font-weight-medium); text-transform: uppercase; letter-spacing: 0.05em; }
    .kpi-widget__value-row { display: flex; align-items: center; gap: var(--space-2); flex-wrap: wrap; }
    .kpi-widget__value { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); line-height: 1; }
    .kpi-widget__sub { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .kpi-widget__link {
      display: inline-block; margin-top: var(--space-3);
      font-size: var(--font-size-xs); color: var(--color-brand-primary);
      text-decoration: none;
      &:hover { text-decoration: underline; }
    }
  `],
})
export class MessagesWidgetComponent {
  @Input() pendingCount = 0;
}
