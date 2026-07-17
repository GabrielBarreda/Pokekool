import { Component, inject, input, output, signal, effect } from '@angular/core';
import { StorageService } from '../storage.service';
import { ToastService } from '@shared/services/toast.service';
import { StorageBinderCardDto } from '../storage.models';
import { PokemonBinderComponent, BinderItem } from '@shared/ui/pokemon-binder/pokemon-binder.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';

/**
 * Overlay plein écran (au-dessus du viewer 3D) affichant les cartes d'un classeur
 * via le composant réutilisable `app-pokemon-binder` (thème Pokémon).
 */
@Component({
  selector: 'app-storage-binder-overlay',
  standalone: true,
  imports: [PokemonBinderComponent, SpinnerComponent],
  template: `
    <div class="ov">
      <header class="ov__head">
        <button class="ov__back" (click)="close.emit()">← Retour carton</button>
        <span class="ov__title">Classeur {{ label() }}</span>
        <span class="ov__count">{{ items().length }} carte(s)</span>
      </header>

      @if (loading()) {
        <div class="ov__center"><app-spinner size="lg" /></div>
      } @else if (items().length === 0) {
        <div class="ov__center ov__empty">Ce classeur ne contient aucune carte référencée.</div>
      } @else {
        <div class="ov__binder">
          <app-pokemon-binder [items]="items()" (itemClick)="onItemClick($event)" />
        </div>
      }
    </div>
  `,
  styles: [`
    .ov { position: absolute; inset: 0; z-index: 30; display: flex; flex-direction: column;
      background: rgba(14,14,20,.92); backdrop-filter: blur(3px);
      animation: ov-in .28s cubic-bezier(.2,.7,.2,1) both; }
    @keyframes ov-in { from { opacity: 0; transform: scale(.96); } to { opacity: 1; transform: scale(1); } }
    .ov__head { display: flex; align-items: center; gap: 1rem; padding: .6rem .9rem; color: #eef; }
    .ov__back { background: var(--color-surface-base); border: 1px solid var(--color-border-default);
      color: var(--color-text-primary); border-radius: var(--radius-md); height: 34px; padding: 0 var(--space-3); cursor: pointer; }
    .ov__title { font-weight: 600; }
    .ov__count { margin-left: auto; font-size: .82rem; color: #aab; }
    .ov__center { flex: 1; display: flex; align-items: center; justify-content: center; color: #ccd; }
    .ov__binder { flex: 1; min-height: 0; overflow: auto; padding: .5rem 1rem 1.5rem; }
  `],
})
export class StorageBinderOverlayComponent {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly binderId = input.required<string>();
  readonly label = input<string>('');

  readonly close = output<void>();
  readonly cardSelected = output<StorageBinderCardDto>();

  readonly loading = signal(true);
  readonly cards = signal<StorageBinderCardDto[]>([]);
  readonly items = signal<BinderItem[]>([]);

  constructor() {
    effect(() => {
      const id = this.binderId();
      if (id) this.load(id);
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.service.getBinderCards(id).subscribe({
      next: cards => {
        this.cards.set(cards);
        this.items.set(cards.map(c => ({
          id: c.locationId,
          imageUrl: c.imageSmall,
          name: c.name,
          imageLargeUrl: c.imageLarge ?? c.imageSmall,
        })));
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.toast.error('Impossible de charger les cartes du classeur.'); },
    });
  }

  onItemClick(item: BinderItem): void {
    const card = this.cards().find(c => c.locationId === item.id);
    if (card) this.cardSelected.emit(card);
  }
}
