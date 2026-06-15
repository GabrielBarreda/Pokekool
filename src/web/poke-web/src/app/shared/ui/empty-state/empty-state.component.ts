import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ButtonComponent } from '../button/button.component';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [ButtonComponent],
  template: `
    <div class="empty">
      @if (icon) { <div class="empty__icon" aria-hidden="true">{{ icon }}</div> }
      <h3 class="empty__title">{{ title }}</h3>
      @if (description) { <p class="empty__description">{{ description }}</p> }
      @if (actionLabel) {
        <button appBtn variant="primary" (click)="actionClicked.emit()">{{ actionLabel }}</button>
      }
    </div>
  `,
  styles: [`
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: var(--space-4);
      padding: var(--space-16) var(--space-8);
      text-align: center;
    }
    .empty__icon { font-size: 48px; opacity: 0.4; }
    .empty__title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0; }
    .empty__description { font-size: var(--font-size-sm); color: var(--color-text-muted); max-width: 320px; margin: 0; }
  `],
})
export class EmptyStateComponent {
  @Input() icon?: string;
  @Input() title = 'Aucun élément';
  @Input() description?: string;
  @Input() actionLabel?: string;
  @Output() actionClicked = new EventEmitter<void>();
}
