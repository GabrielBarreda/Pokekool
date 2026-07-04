import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CardComponent } from '@shared/ui/card/card.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';

/**
 * Widget générique "nouvelles entrées" — réutilisable pour cartes TCG,
 * sets, utilisateurs, etc.
 *
 * @Input() title       — ex: "Nouvelles cartes"
 * @Input() icon        — emoji ou texte affiché en icône
 * @Input() count       — nombre d'éléments (null = inconnu / loading)
 * @Input() items       — liste de labels à afficher (ex: noms de sets)
 * @Input() emptyMessage — message si count === 0 ou null
 * @Input() linkLabel   — label du lien de navigation
 * @Input() linkRoute   — route Angular (ex: '/bourse')
 * @Input() loading     — affiche un état d'attente
 */
@Component({
  selector: 'app-new-items-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, EmptyStateComponent, BadgeComponent, RouterLink],
  template: `
    <app-card>
      <div class="new-items">
        <div class="new-items__header">
          <span class="new-items__icon">{{ icon }}</span>
          <span class="new-items__title">{{ title }}</span>
          @if (count !== null && count > 0) {
            <app-badge variant="success">+{{ count }}</app-badge>
          }
        </div>

        @if (loading) {
          <div class="new-items__loading">
            <span class="new-items__loading-dot"></span>
            <span class="new-items__loading-text">En cours de récupération…</span>
          </div>
        } @else if (!count || count === 0) {
          <p class="new-items__empty">{{ emptyMessage }}</p>
        } @else {
          <div class="new-items__body">
            <span class="new-items__count">{{ count }} éléments</span>
            @if (items.length > 0) {
              <ul class="new-items__list">
                @for (item of items; track item) {
                  <li class="new-items__list-item">{{ item }}</li>
                }
              </ul>
            }
          </div>
        }

        @if (linkRoute && linkLabel && count && count > 0) {
          <a [routerLink]="linkRoute" class="new-items__link">{{ linkLabel }} →</a>
        }
      </div>
    </app-card>
  `,
  styles: [`
    .new-items { display: flex; flex-direction: column; gap: var(--space-3); }

    .new-items__header {
      display: flex; align-items: center; gap: var(--space-2); flex-wrap: wrap;
    }
    .new-items__icon  { font-size: 20px; flex-shrink: 0; }
    .new-items__title {
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      text-transform: uppercase; letter-spacing: 0.05em;
      color: var(--color-text-muted);
      flex: 1;
    }

    .new-items__loading {
      display: flex; align-items: center; gap: var(--space-2);
      padding: var(--space-2) 0;
    }
    .new-items__loading-dot {
      width: 8px; height: 8px;
      border-radius: 50%;
      background: var(--color-brand-primary);
      animation: pulse 1.2s ease-in-out infinite;
    }
    @keyframes pulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50%       { opacity: 0.4; transform: scale(0.8); }
    }
    .new-items__loading-text {
      font-size: var(--font-size-xs); color: var(--color-text-muted); font-style: italic;
    }

    .new-items__empty {
      font-size: var(--font-size-xs); color: var(--color-text-muted);
      line-height: 1.5; margin: 0;
    }

    .new-items__body { display: flex; flex-direction: column; gap: var(--space-2); }
    .new-items__count {
      font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary); line-height: 1;
    }
    .new-items__list {
      list-style: none; margin: 0; padding: 0;
      display: flex; flex-direction: column; gap: 2px;
    }
    .new-items__list-item {
      font-size: var(--font-size-xs); color: var(--color-text-secondary);
      padding: 2px 0;
      &::before { content: '• '; color: var(--color-brand-primary); }
    }

    .new-items__link {
      display: inline-block;
      font-size: var(--font-size-xs); color: var(--color-brand-primary);
      text-decoration: none;
      &:hover { text-decoration: underline; }
    }
  `],
})
export class NewItemsWidgetComponent {
  @Input() title        = 'Nouvelles entrées';
  @Input() icon         = '🆕';
  @Input() count: number | null = null;
  @Input() items: string[]      = [];
  @Input() emptyMessage = 'Aucune nouvelle entrée.';
  @Input() linkLabel    = 'Voir tout';
  @Input() linkRoute    = '/';
  @Input() loading      = false;
}
