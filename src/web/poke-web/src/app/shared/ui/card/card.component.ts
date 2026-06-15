import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-card',
  standalone: true,
  template: `
    @if (title || headerAction) {
      <div class="card__header">
        @if (title) { <h3 class="card__title">{{ title }}</h3> }
        @if (headerAction) { <div class="card__header-action"><ng-content select="[cardHeaderAction]" /></div> }
      </div>
    }
    <div class="card__body"><ng-content /></div>
  `,
  styles: [`
    :host {
      display: block;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-sm);
    }
    .card__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: var(--space-4) var(--space-5);
      border-bottom: 1px solid var(--color-border-subtle);
    }
    .card__title {
      font-size: var(--font-size-md);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
      margin: 0;
    }
    .card__body { padding: var(--space-5); }
  `],
})
export class CardComponent {
  @Input() title?: string;
  @Input() headerAction = false;
}
