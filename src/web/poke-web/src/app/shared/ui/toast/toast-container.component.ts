import { Component, inject } from '@angular/core';
import { NgFor, NgClass } from '@angular/common';
import { ToastService, Toast } from '@shared/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [NgFor, NgClass],
  template: `
    <div class="toast-container" role="region" aria-live="polite" aria-label="Notifications">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          class="toast"
          [ngClass]="'toast--' + toast.type"
          role="alert"
        >
          <span class="toast__icon" aria-hidden="true">
            @switch (toast.type) {
              @case ('success') { ✓ }
              @case ('danger') { ✕ }
              @case ('warning') { ⚠ }
              @default { ℹ }
            }
          </span>
          <span class="toast__message">{{ toast.message }}</span>
          <button
            class="toast__close"
            (click)="toastService.dismiss(toast.id)"
            aria-label="Fermer"
          >✕</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      bottom: var(--space-6);
      right: var(--space-6);
      z-index: var(--z-toast);
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      max-width: 380px;
      width: 100%;
    }

    .toast {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-lg);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      animation: slideIn var(--duration-normal) var(--easing-decelerate);

      &--success {
        background: var(--color-semantic-success-subtle);
        color: var(--color-semantic-success);
        border: 1px solid var(--color-semantic-success);
      }
      &--danger {
        background: var(--color-semantic-danger-subtle);
        color: var(--color-semantic-danger);
        border: 1px solid var(--color-semantic-danger);
      }
      &--warning {
        background: var(--color-semantic-warning-subtle);
        color: var(--color-semantic-warning);
        border: 1px solid var(--color-semantic-warning);
      }
      &--info {
        background: var(--color-semantic-info-subtle);
        color: var(--color-semantic-info);
        border: 1px solid var(--color-semantic-info);
      }
    }

    .toast__icon { font-size: var(--font-size-md); flex-shrink: 0; }
    .toast__message { flex: 1; }
    .toast__close {
      background: none;
      border: none;
      cursor: pointer;
      color: inherit;
      opacity: 0.7;
      padding: var(--space-1);
      border-radius: var(--radius-sm);
      flex-shrink: 0;
      line-height: 1;
      font-size: var(--font-size-sm);
      &:hover { opacity: 1; }
    }

    @keyframes slideIn {
      from { transform: translateX(100%); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
  `],
})
export class ToastContainerComponent {
  readonly toastService = inject(ToastService);
}
