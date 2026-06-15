import { Component, Input } from '@angular/core';

export type BadgeVariant = 'success' | 'danger' | 'warning' | 'info' | 'neutral' | 'brand';

@Component({
  selector: 'app-badge',
  standalone: true,
  template: `<ng-content />`,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      gap: var(--space-1);
      padding: 2px var(--space-2);
      border-radius: var(--radius-full);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      white-space: nowrap;
      line-height: var(--line-height-snug);
    }
    :host(.badge--success) { background: var(--color-semantic-success-subtle); color: var(--color-semantic-success); }
    :host(.badge--danger)  { background: var(--color-semantic-danger-subtle);  color: var(--color-semantic-danger); }
    :host(.badge--warning) { background: var(--color-semantic-warning-subtle); color: var(--color-semantic-warning); }
    :host(.badge--info)    { background: var(--color-semantic-info-subtle);    color: var(--color-semantic-info); }
    :host(.badge--neutral) { background: var(--color-neutral-100);             color: var(--color-text-secondary); }
    :host(.badge--brand)   { background: var(--color-brand-primary-subtle);    color: var(--color-brand-primary); }
  `],
  host: { '[class]': '"badge badge--" + variant' },
})
export class BadgeComponent {
  @Input() variant: BadgeVariant = 'neutral';
}
