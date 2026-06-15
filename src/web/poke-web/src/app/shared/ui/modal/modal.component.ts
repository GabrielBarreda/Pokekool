import {
  Component,
  input,
  output,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <!-- Backdrop -->
    <div class="modal-backdrop" (click)="onBackdropClick()"></div>

    <!-- Panel -->
    <div class="modal-panel" [style.max-width]="width()">
      <!-- Header -->
      <div class="modal-header">
        <h2 class="modal-title">{{ title() }}</h2>
        <button class="modal-close" (click)="closed.emit()" aria-label="Fermer">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M18 6 6 18M6 6l12 12"/>
          </svg>
        </button>
      </div>

      <!-- Body -->
      <div class="modal-body">
        <ng-content />
      </div>
    </div>
  `,
  styles: [`
    :host {
      position: fixed;
      inset: 0;
      z-index: 1000;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: var(--space-4);
    }

    .modal-backdrop {
      position: absolute;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
      backdrop-filter: blur(2px);
    }

    .modal-panel {
      position: relative;
      width: 100%;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-xl);
      display: flex;
      flex-direction: column;
      max-height: calc(100vh - var(--space-8));
      overflow: hidden;
      animation: modal-in 0.15s ease-out;
    }

    @keyframes modal-in {
      from { opacity: 0; transform: scale(0.96) translateY(-8px); }
      to   { opacity: 1; transform: scale(1) translateY(0); }
    }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: var(--space-5) var(--space-6);
      border-bottom: 1px solid var(--color-border-subtle);
      flex-shrink: 0;
    }

    .modal-title {
      font-size: var(--font-size-lg);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
      margin: 0;
    }

    .modal-close {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border: none;
      background: transparent;
      border-radius: var(--radius-md);
      color: var(--color-text-muted);
      cursor: pointer;
      &:hover {
        background: var(--color-neutral-100);
        color: var(--color-text-primary);
      }
    }

    .modal-body {
      padding: var(--space-6);
      overflow-y: auto;
    }
  `],
})
export class ModalComponent {
  readonly title = input<string>('');
  readonly width = input<string>('640px');
  readonly closeOnBackdrop = input<boolean>(true);

  readonly closed = output<void>();

  onBackdropClick(): void {
    if (this.closeOnBackdrop()) {
      this.closed.emit();
    }
  }
}
