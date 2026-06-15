import { Component, Input } from '@angular/core';

export type SpinnerSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-spinner',
  standalone: true,
  template: `<span class="spinner" [attr.aria-label]="label" role="status"></span>`,
  styles: [`
    .spinner {
      display: inline-block;
      border-radius: 50%;
      border: 2px solid var(--color-border-default);
      border-top-color: var(--color-brand-primary);
      animation: spin 0.7s linear infinite;
    }
    :host(.spinner--sm) .spinner { width: 16px; height: 16px; }
    :host(.spinner--md) .spinner { width: 24px; height: 24px; }
    :host(.spinner--lg) .spinner { width: 40px; height: 40px; border-width: 3px; }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
  host: { '[class]': '"spinner--" + size' },
})
export class SpinnerComponent {
  @Input() size: SpinnerSize = 'md';
  @Input() label = 'Chargement…';
}
