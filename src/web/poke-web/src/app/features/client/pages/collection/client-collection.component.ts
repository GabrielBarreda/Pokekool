import { Component, OnInit, HostListener, ViewChild, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CollectionService } from '../../../collection/collection.service';
import { CollectionCardDto, CollectionFilters, CollectionStatsDto } from '../../../collection/collection.models';
import { ClientService } from '../../client.service';
import { CollectionValueDto } from '../../client.models';
import { ThemeStore } from '@core/theme/theme.store';
import { PokemonBinderComponent, BinderItem } from '@shared/ui/pokemon-binder/pokemon-binder.component';

@Component({
  selector: 'app-client-collection',
  standalone: true,
  imports: [FormsModule, PokemonBinderComponent],
  template: `
    <header class="page-head">
      <div>
        <h1 class="page-head__title">Ma Collection</h1>
        <p class="page-head__sub">Gérez les cartes que vous possédez.</p>
      </div>
    </header>

    <!-- Stats -->
    <section class="stats">
      <div class="stat">
        <span class="stat__value">{{ stats()?.ownedCards ?? 0 }} / {{ stats()?.totalCards ?? 0 }}</span>
        <span class="stat__label">Cartes possédées</span>
        <div class="progress"><div class="progress__bar" [style.width.%]="completion()"></div></div>
        <span class="stat__hint">{{ completion() }}% de la base</span>
      </div>
      <div class="stat">
        <span class="stat__value">{{ formatMoney(value()?.totalValue) }}</span>
        <span class="stat__label">Valeur estimée ({{ value()?.currency ?? 'EUR' }})</span>
        <span class="stat__hint">{{ value()?.cardCount ?? 0 }} cartes valorisées</span>
      </div>
    </section>

    <!-- Filters -->
    <section class="filters">
      <input class="input" type="search" placeholder="Rechercher une carte…"
             [ngModel]="search()" (ngModelChange)="onSearch($event)" />
      <label class="check">
        <input type="checkbox" [ngModel]="ownedOnly()" (ngModelChange)="onOwnedOnly($event)" />
        Possédées uniquement
      </label>
      <select class="input input--sm" [ngModel]="sortBy()" (ngModelChange)="onSort($event)">
        <option value="name_asc">Nom (A→Z)</option>
        <option value="name_desc">Nom (Z→A)</option>
        <option value="rarity">Rareté</option>
        <option value="number">Numéro</option>
      </select>
    </section>

    <!-- Grid -->
    @if (loading()) {
      <p class="muted">Chargement…</p>
    } @else if (cards().length === 0) {
      <p class="muted">Aucune carte trouvée.</p>
    } @else {
      @if (themeStore.isPokemon()) {
        <app-pokemon-binder
          #binder
          [items]="binderItems()"
          (itemClick)="onBinderItemClick($event)"
          (itemContextMenu)="onBinderContextMenu($event)"
        />
      } @else {
        <div class="grid">
          @for (card of cards(); track card.id) {
            <button type="button" class="card" [class.card--owned]="card.isOwned"
                    (click)="toggleOwned(card)" (contextmenu)="onContextMenu($event, card)" [title]="card.name">
              @if (card.imageSmall) {
                <img [src]="card.imageSmall" [alt]="card.name" loading="lazy" />
              } @else {
                <div class="card__placeholder">{{ card.name }}</div>
              }
              <span class="card__check" [class.is-on]="card.isOwned">✓</span>
              <span class="card__name">{{ card.name }}</span>
            </button>
          }
        </div>
      }

      @if (totalPages() > 1) {
        <div class="pager">
          <button class="btn" [disabled]="page() <= 1" (click)="goTo(page() - 1)">Précédent</button>
          <span>Page {{ page() }} / {{ totalPages() }}</span>
          <button class="btn" [disabled]="page() >= totalPages()" (click)="goTo(page() + 1)">Suivant</button>
        </div>
      }
    }

    <!-- Menu contextuel -->
    @if (contextMenu(); as ctx) {
      <div class="ctx-menu"
           [style.left.px]="ctx.x"
           [style.top.px]="ctx.y"
           (click)="$event.stopPropagation()">
        @if (themeStore.isPokemon()) {
          <button class="ctx-item" (click)="onVisualizeAction(ctx.card)">👁 Visualiser</button>
        }
        @if (ctx.card.isOwned) {
          <button class="ctx-item" (click)="requestService(ctx.card, 'restoration')">🔧 Restauration</button>
          <button class="ctx-item" (click)="requestService(ctx.card, 'pre_grading')">🏅 Pré-gradation</button>
        }
      </div>
    }
  `,
  styles: [`
    .page-head { margin-bottom: var(--space-5); }
    .page-head__title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); margin: 0; }
    .page-head__sub { color: var(--color-text-muted); margin: var(--space-1) 0 0; }

    .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: var(--space-4); margin-bottom: var(--space-5); }
    .stat { background: var(--color-surface-base); border: 1px solid var(--color-border-subtle); border-radius: var(--radius-lg); padding: var(--space-4); display: flex; flex-direction: column; gap: var(--space-1); }
    .stat__value { font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-brand-primary); }
    .stat__label { font-size: var(--font-size-sm); color: var(--color-text-secondary); }
    .stat__hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .progress { height: 6px; background: var(--color-surface-sunken); border-radius: 999px; overflow: hidden; margin-top: var(--space-1); }
    .progress__bar { height: 100%; background: var(--color-brand-primary); transition: width var(--duration-normal); }

    .filters { display: flex; flex-wrap: wrap; gap: var(--space-3); align-items: center; margin-bottom: var(--space-4); }
    .input { height: 38px; padding: 0 var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); color: var(--color-text-primary); font-size: var(--font-size-sm); }
    .input:not(.input--sm) { flex: 1; min-width: 200px; }
    .check { display: flex; align-items: center; gap: var(--space-2); font-size: var(--font-size-sm); color: var(--color-text-secondary); }

    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: var(--space-3); }
    .card { position: relative; border: 1px solid var(--color-border-subtle); border-radius: var(--radius-md); background: var(--color-surface-base); padding: var(--space-2); cursor: pointer; display: flex; flex-direction: column; gap: var(--space-1); opacity: 0.55; transition: opacity var(--duration-fast), border-color var(--duration-fast); }
    .card--owned { opacity: 1; border-color: var(--color-brand-primary); }
    .card img, .card__placeholder { width: 100%; aspect-ratio: 3/4; object-fit: contain; border-radius: var(--radius-sm); }
    .card__placeholder { display: flex; align-items: center; justify-content: center; background: var(--color-surface-sunken); font-size: var(--font-size-xs); text-align: center; padding: var(--space-2); }
    .card__name { font-size: var(--font-size-xs); color: var(--color-text-secondary); text-align: center; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .card__check { position: absolute; top: 6px; right: 6px; width: 20px; height: 20px; border-radius: 50%; display: flex; align-items: center; justify-content: center; background: var(--color-surface-sunken); color: transparent; font-size: 12px; }
    .card__check.is-on { background: var(--color-brand-primary); color: #fff; }

    .pager { display: flex; align-items: center; justify-content: center; gap: var(--space-4); margin-top: var(--space-5); font-size: var(--font-size-sm); }
    .btn { padding: var(--space-2) var(--space-4); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); cursor: pointer; }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .muted { color: var(--color-text-muted); }

    /* Context menu */
    .ctx-menu { position: fixed; z-index: 1000; min-width: 190px; background: var(--color-surface-raised); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); box-shadow: var(--shadow-lg); overflow: hidden; }
    .ctx-item { display: flex; align-items: center; gap: var(--space-2); padding: var(--space-2) var(--space-3); font-size: var(--font-size-sm); color: var(--color-text-primary); cursor: pointer; border: none; background: none; width: 100%; text-align: left; }
    .ctx-item:hover { background: var(--color-neutral-100); }
  `],
})
export class ClientCollectionComponent implements OnInit {
  private readonly collectionService = inject(CollectionService);
  private readonly clientService = inject(ClientService);
  private readonly router = inject(Router);
  readonly themeStore = inject(ThemeStore);

  // ── Thème Pokémon — classeur ───────────────────────────────────
  @ViewChild('binder') binderRef?: PokemonBinderComponent;

  readonly binderItems = computed<BinderItem[]>(() =>
    this.cards().map(card => ({
      id: card.id,
      imageUrl: card.imageSmall,
      imageLargeUrl: card.imageLarge ?? card.imageSmall,
      name: card.name,
      owned: card.isOwned,
    }))
  );

  readonly contextMenu = signal<{ card: CollectionCardDto; x: number; y: number } | null>(null);

  readonly cards = signal<CollectionCardDto[]>([]);
  readonly stats = signal<CollectionStatsDto | null>(null);
  readonly value = signal<CollectionValueDto | null>(null);
  readonly loading = signal(false);

  readonly search = signal('');
  readonly ownedOnly = signal(false);
  readonly sortBy = signal<CollectionFilters['sortBy']>('name_asc');
  readonly page = signal(1);
  readonly totalPages = signal(1);

  readonly completion = computed(() => {
    const s = this.stats();
    if (!s || s.totalCards === 0) return 0;
    return Math.round((s.ownedCards / s.totalCards) * 100);
  });

  private searchDebounce?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.loadStats();
    this.loadValue();
    this.loadCards();
  }

  formatMoney(v?: number | null): string {
    return (v ?? 0).toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  private loadStats(): void {
    this.collectionService.getStats().subscribe(s => this.stats.set(s));
  }
  private loadValue(): void {
    this.clientService.getCollectionValue().subscribe(v => this.value.set(v));
  }

  private loadCards(): void {
    this.loading.set(true);
    const filters: CollectionFilters = {
      search: this.search(),
      setId: null,
      rarity: null,
      year: null,
      ownedOnly: this.ownedOnly(),
      favoritesOnly: false,
      sortBy: this.sortBy(),
      page: this.page(),
      pageSize: 60,
    };
    this.collectionService.getCards(filters).subscribe({
      next: res => {
        this.cards.set(res.items);
        this.totalPages.set(res.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onSearch(v: string): void {
    this.search.set(v);
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => { this.page.set(1); this.loadCards(); }, 300);
  }
  onOwnedOnly(v: boolean): void { this.ownedOnly.set(v); this.page.set(1); this.loadCards(); }
  onSort(v: CollectionFilters['sortBy']): void { this.sortBy.set(v); this.loadCards(); }
  goTo(p: number): void { this.page.set(p); this.loadCards(); }

  @HostListener('document:click')
  closeContextMenu(): void { this.contextMenu.set(null); }

  @HostListener('document:keydown.escape')
  onEscape(): void { this.contextMenu.set(null); }

  onContextMenu(event: MouseEvent, card: CollectionCardDto): void {
    // En thème par défaut, le menu ne sert qu'aux cartes possédées (services
    // restauration/pré-gradation). En thème Pokémon il s'ouvre aussi pour les
    // cartes non possédées afin de proposer "Visualiser".
    if (!card.isOwned && !this.themeStore.isPokemon()) return;
    event.preventDefault();
    event.stopPropagation();
    const x = Math.min(event.clientX, window.innerWidth  - 200);
    const y = Math.min(event.clientY, window.innerHeight - 100);
    this.contextMenu.set({ card, x, y });
  }

  // ── Thème Pokémon — classeur ───────────────────────────────────
  onBinderItemClick(item: BinderItem): void {
    const card = this.cards().find(c => c.id === item.id);
    if (card) this.toggleOwned(card);
  }

  onBinderContextMenu({ item, event }: { item: BinderItem; event: MouseEvent }): void {
    const card = this.cards().find(c => c.id === item.id);
    if (!card) return;
    this.onContextMenu(event, card);
  }

  onVisualizeAction(card: CollectionCardDto): void {
    this.contextMenu.set(null);
    this.binderRef?.viewItem(card.id);
  }

  requestService(card: CollectionCardDto, service: 'restoration' | 'pre_grading'): void {
    this.contextMenu.set(null);
    this.router.navigate(['/client/rdv/new'], {
      queryParams: { service, cardId: card.id, cardName: card.name },
    });
  }

  toggleOwned(card: CollectionCardDto): void {
    // Optimistic update
    const prev = card.isOwned;
    this.cards.update(list => list.map(c => c.id === card.id ? { ...c, isOwned: !prev } : c));
    this.collectionService.toggleOwned(card.id).subscribe({
      next: () => { this.loadStats(); this.loadValue(); },
      error: () => this.cards.update(list => list.map(c => c.id === card.id ? { ...c, isOwned: prev } : c)),
    });
  }
}
