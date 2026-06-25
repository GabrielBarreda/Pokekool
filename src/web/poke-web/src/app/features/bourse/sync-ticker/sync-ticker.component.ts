import {
  Component, OnInit, OnDestroy, ChangeDetectionStrategy, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { MarketService } from '../market.service';
import { SyncProgressEvent } from '../market.models';

const MAX_EVENTS = 60;

@Component({
  selector: 'app-sync-ticker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <div class="ticker-wrap" role="status" aria-live="polite">
      <span class="ticker-label">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
          <polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/>
          <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
        </svg>
        SYNC EN COURS
      </span>
      <div class="ticker-track">
        <div class="ticker-inner" [style.animation-duration]="animDuration()">
          @for (evt of events(); track $index) {
            <span class="ticker-item" [class.ticker-item--error]="!evt.success">
              <span class="ticker-set">{{ evt.setName }}</span>
              —
              <span class="ticker-card">{{ evt.cardName }} #{{ evt.cardNumber }}</span>
              @if (evt.success && evt.grades.length) {
                <span class="ticker-grades">{{ evt.grades.join(' / ') }} ✅</span>
              } @else if (!evt.success) {
                <span class="ticker-grades">❌</span>
              }
              &nbsp;&nbsp;&nbsp;·&nbsp;&nbsp;&nbsp;
            </span>
          }
          <!-- Double pour boucle fluide -->
          @for (evt of events(); track $index) {
            <span class="ticker-item" [class.ticker-item--error]="!evt.success">
              <span class="ticker-set">{{ evt.setName }}</span>
              —
              <span class="ticker-card">{{ evt.cardName }} #{{ evt.cardNumber }}</span>
              @if (evt.success && evt.grades.length) {
                <span class="ticker-grades">{{ evt.grades.join(' / ') }} ✅</span>
              } @else if (!evt.success) {
                <span class="ticker-grades">❌</span>
              }
              &nbsp;&nbsp;&nbsp;·&nbsp;&nbsp;&nbsp;
            </span>
          }
        </div>
      </div>
      <span class="ticker-count">{{ events().length }} traité(s)</span>
    </div>
  `,
  styles: [`
    .ticker-wrap {
      display: flex; align-items: center; gap: var(--space-3);
      background: var(--color-surface-raised);
      border-bottom: 1px solid var(--color-border-default);
      padding: 0 var(--space-4);
      height: 36px; overflow: hidden;
      font-size: var(--font-size-xs);
    }

    .ticker-label {
      display: flex; align-items: center; gap: 5px;
      flex-shrink: 0;
      font-size: 10px; font-weight: var(--font-weight-semibold);
      letter-spacing: 0.08em; color: var(--color-brand-primary);
      text-transform: uppercase;
      svg { animation: spin 2s linear infinite; }
      @keyframes spin { to { transform: rotate(360deg) } }
    }

    .ticker-track {
      flex: 1; overflow: hidden; position: relative;
      &::before, &::after {
        content: ''; position: absolute; top: 0; bottom: 0; width: 24px; z-index: 1;
        pointer-events: none;
      }
      &::before { left: 0; background: linear-gradient(to right, var(--color-surface-raised), transparent); }
      &::after  { right: 0; background: linear-gradient(to left, var(--color-surface-raised), transparent); }
    }

    .ticker-inner {
      display: inline-flex; align-items: center; white-space: nowrap;
      animation: scroll linear infinite;
      @keyframes scroll { from { transform: translateX(0) } to { transform: translateX(-50%) } }
    }

    .ticker-item {
      color: var(--color-text-secondary);
      &--error { color: var(--color-semantic-error); }
    }

    .ticker-set   { color: var(--color-text-muted); }
    .ticker-card  { color: var(--color-text-primary); font-weight: var(--font-weight-medium); }
    .ticker-grades { color: var(--color-brand-primary); margin-left: 4px; }

    .ticker-count {
      flex-shrink: 0;
      color: var(--color-text-muted); font-size: 10px;
    }
  `],
})
export class SyncTickerComponent implements OnInit, OnDestroy {
  readonly events = signal<SyncProgressEvent[]>([]);

  readonly animDuration = () => {
    const n = Math.max(this.events().length, 1);
    return `${n * 4}s`;  // ~4s par item
  };

  private sub?: Subscription;

  constructor(private svc: MarketService) {}

  ngOnInit(): void {
    this.sub = this.svc.connectProgressStream().subscribe(evt => {
      this.events.update(prev => {
        const next = [evt, ...prev];
        return next.length > MAX_EVENTS ? next.slice(0, MAX_EVENTS) : next;
      });
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
