import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Backdrop -->
    <div class="backdrop" (click)="cancelled.emit()"></div>

    <!-- Dialog -->
    <div class="dialog" role="alertdialog" [attr.aria-labelledby]="'cd-title'" [attr.aria-describedby]="'cd-desc'">
      <div class="dialog-icon" [class]="'icon-' + variant()">
        @if (variant() === 'danger') {
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
          </svg>
        } @else {
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
        }
      </div>

      <h3 class="dialog-title" id="cd-title">{{ title() }}</h3>
      <p class="dialog-desc" id="cd-desc">{{ message() }}</p>

      <div class="dialog-actions">
        <button class="btn-ghost" (click)="cancelled.emit()">Annuler</button>
        <button class="btn-confirm" [class]="'btn-' + variant()" (click)="confirmed.emit()">
          {{ confirmLabel() }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    :host {
      position: fixed;
      inset: 0;
      z-index: 1100;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: var(--space-4);
    }
    .backdrop {
      position: absolute;
      inset: 0;
      background: rgba(0,0,0,0.5);
      backdrop-filter: blur(2px);
    }
    .dialog {
      position: relative;
      width: 100%;
      max-width: 440px;
      background: var(--color-surface-base);
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-xl);
      padding: var(--space-6);
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: var(--space-3);
      animation: dialog-in 0.15s ease-out;
    }
    @keyframes dialog-in {
      from { opacity: 0; transform: scale(0.95); }
      to   { opacity: 1; transform: scale(1); }
    }
    .dialog-icon {
      width: 52px;
      height: 52px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .icon-danger { background: var(--color-semantic-danger-subtle); color: var(--color-semantic-danger); }
    .icon-warning { background: var(--color-semantic-warning-subtle); color: var(--color-semantic-warning); }
    .dialog-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0; }
    .dialog-desc { font-size: var(--font-size-sm); color: var(--color-text-secondary); margin: 0; }
    .dialog-actions { display: flex; gap: var(--space-3); margin-top: var(--space-2); }
    .btn-ghost {
      height: 38px; padding: 0 var(--space-5);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: transparent; color: var(--color-text-secondary);
      font-size: var(--font-size-sm); cursor: pointer;
      &:hover { background: var(--color-neutral-100); }
    }
    .btn-confirm { height: 38px; padding: 0 var(--space-5); border: none; border-radius: var(--radius-md); font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); cursor: pointer; }
    .btn-danger { background: var(--color-semantic-danger); color: white; &:hover { opacity: 0.9; } }
    .btn-warning { background: var(--color-semantic-warning); color: white; &:hover { opacity: 0.9; } }
  `],
})
export class ConfirmDialogComponent {
  readonly title = input<string>('Confirmation');
  readonly message = input<string>('');
  readonly confirmLabel = input<string>('Confirmer');
  readonly variant = input<'danger' | 'warning'>('danger');

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
