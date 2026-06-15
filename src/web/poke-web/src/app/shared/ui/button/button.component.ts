import { Component, Input, HostBinding } from '@angular/core';
import { NgClass } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'button[appBtn], a[appBtn]',
  standalone: true,
  imports: [NgClass],
  template: `
    @if (loading) {
      <span class="btn__spinner" aria-hidden="true"></span>
    }
    <ng-content />
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
      border-radius: var(--radius-md);
      font-family: var(--font-family-sans);
      font-weight: var(--font-weight-medium);
      cursor: pointer;
      transition: background-color var(--duration-fast) var(--easing-standard),
                  border-color var(--duration-fast) var(--easing-standard),
                  opacity var(--duration-fast) var(--easing-standard);
      border: 1px solid transparent;
      text-decoration: none;
      white-space: nowrap;
      user-select: none;
    }

    /* Sizes */
    :host(.btn--sm) { padding: var(--space-1) var(--space-3); font-size: var(--font-size-sm); }
    :host(.btn--md) { padding: var(--space-2) var(--space-4); font-size: var(--font-size-sm); }
    :host(.btn--lg) { padding: var(--space-3) var(--space-6); font-size: var(--font-size-md); }

    /* Variants */
    :host(.btn--primary) {
      background: var(--color-brand-primary);
      color: var(--color-brand-on-primary);
      border-color: var(--color-brand-primary);
      &:hover { background: var(--color-brand-primary-hover); border-color: var(--color-brand-primary-hover); }
      &:active { background: var(--color-brand-primary-active); }
    }
    :host(.btn--secondary) {
      background: var(--color-surface-base);
      color: var(--color-text-primary);
      border-color: var(--color-border-default);
      &:hover { background: var(--color-surface-raised); }
    }
    :host(.btn--ghost) {
      background: transparent;
      color: var(--color-text-secondary);
      border-color: transparent;
      &:hover { background: var(--color-surface-raised); color: var(--color-text-primary); }
    }
    :host(.btn--danger) {
      background: var(--color-semantic-danger);
      color: #fff;
      border-color: var(--color-semantic-danger);
      &:hover { opacity: 0.9; }
    }

    /* States */
    :host([disabled]), :host(.btn--loading) {
      opacity: 0.5;
      pointer-events: none;
      cursor: not-allowed;
    }

    /* Spinner */
    .btn__spinner {
      display: inline-block;
      width: 14px;
      height: 14px;
      border: 2px solid currentColor;
      border-top-color: transparent;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
      flex-shrink: 0;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
  host: {
    '[class]': '"btn btn--" + variant + " btn--" + size + (loading ? " btn--loading" : "")',
    '[attr.aria-busy]': 'loading',
    '[attr.disabled]': 'disabled || loading ? true : null',
  },
})
export class ButtonComponent {
  @Input() variant: ButtonVariant = 'primary';
  @Input() size: ButtonSize = 'md';
  @Input() loading = false;
  @Input() disabled = false;
}
