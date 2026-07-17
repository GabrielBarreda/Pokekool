import { Component, input, signal, output } from '@angular/core';
import { StorageIconComponent, StorageIconName } from './storage-icon.component';

/**
 * Section repliable (accordéon) pour la colonne gauche du Stockage.
 * - Icône fournie via `icon` (carton / classeur / meuble), rendue par app-storage-icon.
 * - Titre + badge de comptage optionnel.
 * - État ouvert/fermé local (signal), initialisé par `open` (défaut : ouvert).
 *
 * Usage :
 *   <app-storage-expander title="Cartons" icon="box" [count]="boxes().length">
 *     <app-storage-box-list ... />
 *   </app-storage-expander>
 */
@Component({
  selector: 'app-storage-expander',
  standalone: true,
  imports: [StorageIconComponent],
  template: `
    <section class="exp" [class.exp--open]="isOpen()">
      <button type="button" class="exp__head" (click)="toggle()"
              [attr.aria-expanded]="isOpen()">
        @if (icon(); as ic) { <app-storage-icon class="exp__lead" [name]="ic" [size]="18" /> }
        <span class="exp__title">{{ title() }}</span>
        @if (count() != null) { <span class="exp__badge">{{ count() }}</span> }
        <svg class="exp__chevron" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
          <path d="M4 6l4 4 4-4" fill="none" stroke="currentColor" stroke-width="1.6"
                stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
      @if (isOpen()) {
        <div class="exp__body"><ng-content></ng-content></div>
      }
    </section>
  `,
  styles: [`
    .exp { border: 1px solid #e2e2e2; border-radius: 12px; background: #fff; overflow: hidden; }
    .exp__head { width: 100%; display: flex; align-items: center; gap: .5rem;
                 padding: .6rem .75rem; background: none; border: none; cursor: pointer;
                 font: inherit; color: var(--color-text-primary, #222); }
    .exp__head:hover { background: #f7f7f7; }
    .exp__lead { flex: 0 0 auto; }
    .exp__title { font-size: .82rem; font-weight: 600; text-transform: uppercase;
                  letter-spacing: .04em; color: var(--color-text-secondary, #555); flex: 1;
                  text-align: left; }
    .exp__badge { display: inline-flex; align-items: center; justify-content: center;
                  min-width: 1.35rem; height: 1.35rem; padding: 0 .35rem; border-radius: 999px;
                  background: #eef1f6; color: #46506b; font-size: .72rem; font-weight: 700; flex: 0 0 auto; }
    .exp__chevron { color: #9aa1ad; flex: 0 0 auto; transition: transform .18s ease; }
    .exp--open .exp__chevron { transform: rotate(180deg); }
    .exp__body { padding: 0 .75rem .8rem; }
  `],
})
export class StorageExpanderComponent {
  readonly title = input.required<string>();
  readonly icon = input<StorageIconName | null>(null);
  readonly count = input<number | null>(null);
  readonly open = input<boolean>(true);

  /** Émis à chaque bascule (utile si le parent veut réagir). */
  readonly toggled = output<boolean>();

  private readonly overridden = signal<boolean | null>(null);
  readonly isOpen = () => this.overridden() ?? this.open();

  toggle(): void {
    const next = !this.isOpen();
    this.overridden.set(next);
    this.toggled.emit(next);
  }
}
