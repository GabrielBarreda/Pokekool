import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CardComponent } from '@shared/ui/card/card.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { RecentlyScrapedCardDto } from '../dashboard.models';

@Component({
  selector: 'app-recently-scraped-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, EmptyStateComponent, RouterLink, DatePipe, DecimalPipe],
  template: `
    <app-card title="Nouveautés scrapées">
      <a routerLink="/bourse" cardHeaderAction class="widget-link">voir tout →</a>

      @if (cards.length === 0) {
        <app-empty-state
          icon="📭"
          title="Aucune donnée importée aujourd'hui"
          description="Le scraping des prix n'a pas encore tourné dans les dernières 24h."
        />
      } @else {
        <div class="scraped-scroll">
          @for (card of cards; track card.cardId) {
            <div class="scraped-card">
              <div class="scraped-card__img-wrap">
                @if (card.imageSmall) {
                  <img [src]="card.imageSmall" [alt]="card.name" class="scraped-card__img" loading="lazy" />
                } @else {
                  <div class="scraped-card__img-placeholder">🃏</div>
                }
              </div>
              <div class="scraped-card__info">
                <span class="scraped-card__name" [title]="card.name">{{ card.name }}</span>
                <span class="scraped-card__set">{{ card.setName }}</span>
                @if (card.priceMarket !== null) {
                  <span class="scraped-card__price">
                    {{ card.priceMarket | number:'1.2-2' }} {{ card.priceCurrency }}
                    @if (card.priceSource) {
                      <span class="scraped-card__source">{{ card.priceSource }}</span>
                    }
                  </span>
                }
                <span class="scraped-card__time">{{ relativeTime(card.scrapedAt) }}</span>
              </div>
            </div>
          }
        </div>
      }
    </app-card>
  `,
  styles: [`
    .widget-link {
      font-size: var(--font-size-sm);
      color: var(--color-brand-primary);
      text-decoration: none;
      &:hover { text-decoration: underline; }
    }
    .scraped-scroll {
      display: flex;
      gap: var(--space-3);
      overflow-x: auto;
      padding-bottom: var(--space-2);
      scrollbar-width: thin;
    }
    .scraped-card {
      display: flex;
      flex-direction: column;
      flex-shrink: 0;
      width: 120px;
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-lg);
      overflow: hidden;
      background: var(--color-surface-raised);
    }
    .scraped-card__img-wrap { height: 90px; background: var(--color-neutral-100); }
    .scraped-card__img { width: 100%; height: 100%; object-fit: contain; }
    .scraped-card__img-placeholder {
      display: flex; align-items: center; justify-content: center;
      height: 100%; font-size: 32px; opacity: 0.3;
    }
    .scraped-card__info {
      display: flex; flex-direction: column; gap: 2px;
      padding: var(--space-2);
    }
    .scraped-card__name {
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .scraped-card__set {
      font-size: 10px; color: var(--color-text-muted);
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .scraped-card__price {
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      color: var(--color-brand-primary);
    }
    .scraped-card__source {
      font-size: 9px; color: var(--color-text-muted);
      display: block;
    }
    .scraped-card__time {
      font-size: 10px; color: var(--color-text-muted);
    }
  `],
})
export class RecentlyScrapedWidgetComponent {
  @Input() cards: RecentlyScrapedCardDto[] = [];

  relativeTime(isoDate: string): string {
    const diff = Date.now() - new Date(isoDate).getTime();
    const hours = Math.floor(diff / 3_600_000);
    if (hours < 1) return 'il y a < 1h';
    if (hours < 24) return `il y a ${hours}h`;
    const days = Math.floor(hours / 24);
    return `il y a ${days}j`;
  }
}
