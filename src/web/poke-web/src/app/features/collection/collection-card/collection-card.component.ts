import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { CollectionCardDto } from '../collection.models';

@Component({
  selector: 'app-collection-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  styles: [`
    :host {
      display: block;
    }

    .card-wrapper {
      position: relative;
      cursor: pointer;
      border-radius: var(--radius-md);
      overflow: hidden;
      background: var(--color-surface-raised);
      border: 1px solid var(--color-border-subtle);
      transition: transform var(--duration-fast) ease, box-shadow var(--duration-fast) ease;
      aspect-ratio: 2.5 / 3.5;
      user-select: none;
    }

    .card-wrapper:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-md);
    }

    .card-wrapper.is-toggling {
      cursor: wait;
      pointer-events: none;
    }

    .card-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
      transition: filter 0.2s ease;
    }

    .card-img.not-owned {
      filter: grayscale(100%) opacity(0.5);
    }

    .card-img.owned {
      filter: none;
    }

    .card-placeholder {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--color-neutral-100);
      color: var(--color-text-muted);
      font-size: var(--font-size-sm);
      transition: filter 0.2s ease;
    }

    .card-placeholder.not-owned {
      filter: grayscale(100%) opacity(0.5);
    }

    /* Overlays */
    .badge-favorite {
      position: absolute;
      top: var(--space-1);
      right: var(--space-1);
      font-size: 14px;
      line-height: 1;
      color: #f59e0b;
      text-shadow: 0 1px 2px rgba(0,0,0,0.5);
    }

    .badge-priority {
      position: absolute;
      top: var(--space-1);
      left: var(--space-1);
      font-size: 12px;
      line-height: 1;
      background: #f97316;
      color: white;
      border-radius: var(--radius-sm);
      padding: 2px 4px;
    }

    .badge-owned {
      position: absolute;
      bottom: var(--space-1);
      right: var(--space-1);
      width: 10px;
      height: 10px;
      border-radius: var(--radius-full);
      background: var(--color-semantic-success);
      border: 1px solid rgba(0,0,0,0.2);
    }

    .spinner-overlay {
      position: absolute;
      inset: 0;
      background: rgba(0,0,0,0.25);
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: var(--radius-md);
    }

    .spinner {
      width: 20px;
      height: 20px;
      border: 2px solid rgba(255,255,255,0.4);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `],
  template: `
    <div
      class="card-wrapper"
      [class.is-toggling]="isToggling"
      (click)="onCardClick()"
      (contextmenu)="onContextMenu($event)"
      [title]="card.displayName || card.name"
    >
      @if (card.imageSmall) {
        <img
          class="card-img"
          [class.not-owned]="!card.isOwned"
          [class.owned]="card.isOwned"
          [src]="card.imageSmall"
          [alt]="card.displayName || card.name"
          loading="lazy"
        />
      } @else {
        <div class="card-placeholder" [class.not-owned]="!card.isOwned">
          {{ card.number }} · {{ card.displayName || card.name }}
        </div>
      }

      @if (card.isFavorite) {
        <span class="badge-favorite" title="Favori">★</span>
      }

      @if (card.isPriority && !card.isOwned) {
        <span class="badge-priority" title="Prioritaire">!</span>
      }

      @if (card.isOwned) {
        <span class="badge-owned" title="Possédée"></span>
      }

      @if (isToggling) {
        <div class="spinner-overlay">
          <div class="spinner"></div>
        </div>
      }
    </div>
  `,
})
export class CollectionCardComponent {
  @Input({ required: true }) card!: CollectionCardDto;
  @Input() isToggling = false;
  @Output() cardClick = new EventEmitter<void>();
  @Output() contextMenu = new EventEmitter<MouseEvent>();

  onCardClick(): void {
    if (!this.isToggling) this.cardClick.emit();
  }

  onContextMenu(event: MouseEvent): void {
    event.preventDefault();
    this.contextMenu.emit(event);
  }
}
