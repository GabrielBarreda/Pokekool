import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CardComponent } from '@shared/ui/card/card.component';
import { AiStatsDto } from '../dashboard.models';

@Component({
  selector: 'app-ai-stats-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent],
  template: `
    <app-card>
      <div class="kpi-widget">
        <span class="kpi-widget__icon">⚡</span>
        <div class="kpi-widget__body">
          <span class="kpi-widget__label">Automatisations IA</span>
          @if (!stats.isEnabled) {
            <span class="kpi-widget__soon">Bientôt disponible</span>
            <span class="kpi-widget__sub">Analyses de gradation et recommandations — Phase 4</span>
          } @else {
            <div class="kpi-widget__stats">
              <span class="kpi-stat">
                <span class="kpi-stat__value">{{ stats.messagesAnsweredToday }}</span>
                <span class="kpi-stat__label">aujourd'hui</span>
              </span>
              <span class="kpi-stat">
                <span class="kpi-stat__value">{{ stats.messagesAnsweredThisWeek }}</span>
                <span class="kpi-stat__label">cette semaine</span>
              </span>
            </div>
          }
        </div>
      </div>
    </app-card>
  `,
  styles: [`
    .kpi-widget {
      display: flex; gap: var(--space-3); align-items: flex-start;
    }
    .kpi-widget__icon { font-size: 24px; line-height: 1; flex-shrink: 0; }
    .kpi-widget__body { display: flex; flex-direction: column; gap: var(--space-1); }
    .kpi-widget__label { font-size: var(--font-size-xs); color: var(--color-text-muted); font-weight: var(--font-weight-medium); text-transform: uppercase; letter-spacing: 0.05em; }
    .kpi-widget__soon { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-secondary); }
    .kpi-widget__sub { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .kpi-widget__stats { display: flex; gap: var(--space-4); margin-top: var(--space-1); }
    .kpi-stat { display: flex; flex-direction: column; gap: 2px; }
    .kpi-stat__value { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .kpi-stat__label { font-size: 10px; color: var(--color-text-muted); }
  `],
})
export class AiStatsWidgetComponent {
  @Input() stats: AiStatsDto = { messagesAnsweredToday: 0, messagesAnsweredThisWeek: 0, isEnabled: false };
}
