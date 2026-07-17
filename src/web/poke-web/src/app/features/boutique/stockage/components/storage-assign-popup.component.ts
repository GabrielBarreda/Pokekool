import {
  Component, inject, input, output, signal, computed, OnInit, effect,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StorageService } from '../storage.service';
import { ToastService } from '@shared/services/toast.service';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { StorageBinderDto, ShopStockDto, PagedResult } from '../storage.models';

type Phase = 'list' | 'assign-label';

/**
 * Popup d'assignation de cartes du stock boutique vers un classeur.
 *
 * Deux chemins :
 *  1. Drag & drop : glisser une carte sur un classeur de la liste latérale (géré dans stockage.component)
 *     — le payload drag est "stock:{ownerId}".
 *  2. Assignation par sélection + choix du classeur cible dans la popup.
 *
 * La popup émet `assigned` quand au moins une assignation a réussi.
 */
@Component({
  selector: 'app-storage-assign-popup',
  standalone: true,
  imports: [FormsModule, SpinnerComponent],
  template: `
    <div class="backdrop" (click)="onBackdropClick($event)">
      <div class="modal" role="dialog" aria-modal="true" aria-label="Assigner depuis le stock boutique">

        <!-- En-tête -->
        <div class="modal__head">
          <h2 class="modal__title">Assigner depuis le stock boutique</h2>
          <button class="modal__close" (click)="close.emit()" aria-label="Fermer">✕</button>
        </div>

        <!-- Barre de recherche + filtre type -->
        <div class="toolbar">
          <input class="search-input" placeholder="Rechercher une carte…"
                 [(ngModel)]="searchQuery" (ngModelChange)="onSearchChange()" />
          <select class="type-select" [(ngModel)]="typeFilter" (ngModelChange)="search()">
            <option value="">Tous types</option>
            <option value="CardRaw">Carte brute</option>
            <option value="CardGraded">Carte gradée</option>
            <option value="Sealed">Scellé</option>
          </select>
          @if (selected().size > 0) {
            <span class="sel-badge">{{ selected().size }} sélectionnée(s)</span>
          }
        </div>

        <!-- Corps -->
        <div class="modal__body">
          @if (loading()) {
            <div class="center"><app-spinner /></div>
          } @else if (result()?.items?.length === 0) {
            <p class="empty">Aucun article dans le stock pour cette recherche.</p>
          } @else {
            <ul class="card-grid">
              @for (item of result()?.items ?? []; track item.id) {
                <li class="card-item"
                    [class.card-item--sel]="selected().has(item.id)"
                    draggable="true"
                    (dragstart)="onDragStart($event, item)"
                    (click)="toggleSelect(item.id)">
                  <div class="card-item__img-wrap">
                    @if (item.cardImage) {
                      <img [src]="item.cardImage" [alt]="item.cardName ?? 'carte'" class="card-item__img" loading="lazy" />
                    } @else {
                      <div class="card-item__no-img">?</div>
                    }
                    @if (selected().has(item.id)) {
                      <div class="card-item__check">✓</div>
                    }
                  </div>
                  <div class="card-item__info">
                    <span class="card-item__name">{{ item.cardName ?? item.productName ?? '—' }}</span>
                    <span class="card-item__meta">
                      @if (item.gradingCompany) { {{ item.gradingCompany }} {{ item.gradeValue }} · }
                      @else if (item.condition) { {{ item.condition }} · }
                      Qté {{ item.quantityAvailable }}
                    </span>
                  </div>
                </li>
              }
            </ul>

            <!-- Pagination -->
            @if ((result()?.totalCount ?? 0) > pageSize) {
              <div class="pagination">
                <button class="pg-btn" [disabled]="page() <= 1" (click)="goPage(page() - 1)">‹</button>
                <span class="pg-info">{{ page() }} / {{ totalPages() }}</span>
                <button class="pg-btn" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">›</button>
              </div>
            }
          }
        </div>

        <!-- Pied : assignation par classeur -->
        <div class="modal__foot">
          @if (selected().size > 0) {
            <div class="assign-row">
              <span class="assign-hint">Assigner {{ selected().size }} carte(s) au classeur :</span>
              <select class="binder-select" [(ngModel)]="targetBinderId">
                <option value="">— Choisir un classeur —</option>
                @for (b of binders(); track b.id) {
                  <option [value]="b.id">{{ b.label }}{{ b.positionInBox != null ? ' (#' + b.positionInBox + ')' : '' }}</option>
                }
              </select>
              <button class="btn-primary"
                      [disabled]="!targetBinderId || saving()"
                      (click)="assignSelected()">
                @if (saving()) { … } @else { Assigner }
              </button>
            </div>
          } @else {
            <p class="foot-hint">
              Sélectionnez des cartes puis choisissez un classeur, ou glissez-déposez une carte sur un classeur de la liste.
            </p>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.55); z-index: 1000;
                display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .modal { background: var(--color-surface-base); border-radius: var(--radius-lg);
             width: min(860px, 96vw); max-height: 90vh; display: flex; flex-direction: column;
             box-shadow: 0 24px 48px rgba(0,0,0,.3); overflow: hidden; }

    .modal__head { display: flex; align-items: center; justify-content: space-between;
                   padding: 1rem 1.25rem; border-bottom: 1px solid var(--color-border-default); }
    .modal__title { margin: 0; font-size: 1.05rem; font-weight: 700; }
    .modal__close { border: none; background: none; color: var(--color-text-secondary);
                    cursor: pointer; font-size: 1.1rem; padding: 0 .3rem; line-height: 1; }
    .modal__close:hover { color: var(--color-text-primary); }

    .toolbar { display: flex; gap: .5rem; align-items: center; padding: .75rem 1.25rem;
               border-bottom: 1px solid var(--color-border-default); flex-wrap: wrap; }
    .search-input { flex: 1; min-width: 180px; height: 36px; padding: 0 .75rem;
                    border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
                    background: var(--color-surface-base); color: var(--color-text-primary);
                    font-size: var(--font-size-sm); }
    .search-input:focus { outline: none; border-color: var(--color-border-focus); }
    .type-select { height: 36px; padding: 0 .5rem; border: 1px solid var(--color-border-default);
                   border-radius: var(--radius-md); background: var(--color-surface-base);
                   color: var(--color-text-primary); font-size: var(--font-size-sm); cursor: pointer; }
    .sel-badge { display: inline-flex; align-items: center; padding: .2rem .55rem; border-radius: 99px;
                 background: var(--color-brand-primary); color: var(--color-brand-on-primary);
                 font-size: .72rem; font-weight: 700; white-space: nowrap; }

    .modal__body { flex: 1; overflow-y: auto; padding: .75rem 1.25rem; }
    .center { display: flex; justify-content: center; padding: 2rem; }
    .empty { color: var(--color-text-secondary); font-size: var(--font-size-sm); text-align: center; padding: 2rem 0; }

    .card-grid { list-style: none; margin: 0; padding: 0;
                 display: grid; grid-template-columns: repeat(auto-fill, minmax(130px, 1fr)); gap: .6rem; }
    .card-item { display: flex; flex-direction: column; border: 2px solid var(--color-border-default);
                 border-radius: var(--radius-md); overflow: hidden; cursor: pointer;
                 transition: border-color .12s, box-shadow .12s; background: var(--color-surface-base); }
    .card-item:hover { border-color: var(--color-brand-primary); box-shadow: 0 0 0 3px rgba(var(--color-brand-primary-rgb, 59 130 246),.12); }
    .card-item--sel { border-color: var(--color-brand-primary); background: #eff6ff; }
    .card-item__img-wrap { position: relative; aspect-ratio: 63/88; background: #f0f0f0;
                           display: flex; align-items: center; justify-content: center; }
    .card-item__img { width: 100%; height: 100%; object-fit: contain; display: block; }
    .card-item__no-img { font-size: 2rem; color: #ccc; }
    .card-item__check { position: absolute; top: 4px; right: 4px; width: 20px; height: 20px;
                        border-radius: 50%; background: var(--color-brand-primary); color: #fff;
                        display: flex; align-items: center; justify-content: center; font-size: .75rem; font-weight: 700; }
    .card-item__info { padding: .35rem .45rem; display: flex; flex-direction: column; gap: .15rem; }
    .card-item__name { font-size: .72rem; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .card-item__meta { font-size: .65rem; color: var(--color-text-secondary); }

    .pagination { display: flex; align-items: center; justify-content: center; gap: .5rem; margin-top: .75rem; }
    .pg-btn { display: inline-flex; align-items: center; justify-content: center; width: 32px; height: 32px;
              border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
              background: var(--color-surface-base); cursor: pointer; font-size: 1.1rem; }
    .pg-btn:disabled { opacity: .4; cursor: not-allowed; }
    .pg-info { font-size: .82rem; color: var(--color-text-secondary); }

    .modal__foot { padding: .75rem 1.25rem; border-top: 1px solid var(--color-border-default);
                   background: var(--color-surface-raised); }
    .assign-row { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; }
    .assign-hint { font-size: .82rem; color: var(--color-text-secondary); white-space: nowrap; }
    .binder-select { flex: 1; min-width: 160px; height: 36px; padding: 0 .5rem;
                     border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
                     background: var(--color-surface-base); color: var(--color-text-primary); font-size: var(--font-size-sm); }
    .btn-primary { display: inline-flex; align-items: center; justify-content: center; gap: 6px;
                   height: 36px; padding: 0 1rem; border-radius: var(--radius-md); font-size: var(--font-size-sm);
                   cursor: pointer; border: 1px solid transparent; white-space: nowrap;
                   background: var(--color-brand-primary); color: var(--color-brand-on-primary); }
    .btn-primary:hover:not(:disabled) { background: var(--color-brand-primary-hover); }
    .btn-primary:disabled { opacity: .55; cursor: not-allowed; }
    .foot-hint { margin: 0; font-size: .8rem; color: var(--color-text-secondary); text-align: center; }
  `],
})
export class StorageAssignPopupComponent implements OnInit {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly binders = input<StorageBinderDto[]>([]);
  readonly selectedBinder = input<StorageBinderDto | null>(null);

  readonly close = output<void>();
  readonly assigned = output<void>();

  readonly result = signal<PagedResult<ShopStockDto> | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly selected = signal<Set<string>>(new Set());
  readonly page = signal(1);
  readonly pageSize = 24;

  searchQuery = '';
  typeFilter = '';
  targetBinderId = '';
  private searchTimer?: ReturnType<typeof setTimeout>;

  readonly totalPages = computed(() => {
    const total = this.result()?.totalCount ?? 0;
    return Math.max(1, Math.ceil(total / this.pageSize));
  });

  constructor() {
    effect(() => {
      const b = this.selectedBinder();
      if (b) this.targetBinderId = b.id;
    });
  }

  ngOnInit(): void { this.search(); }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page.set(1); this.search(); }, 350);
  }

  search(): void {
    this.loading.set(true);
    this.service.getShopStock({
      type: this.typeFilter || undefined,
      search: this.searchQuery || undefined,
      page: this.page(),
      pageSize: this.pageSize,
    }).subscribe({
      next: r => { this.result.set(r); this.loading.set(false); },
      error: () => { this.loading.set(false); this.toast.error('Impossible de charger le stock.'); },
    });
  }

  goPage(p: number): void {
    this.page.set(p);
    this.search();
  }

  toggleSelect(id: string): void {
    this.selected.update(s => {
      const next = new Set(s);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  }

  onDragStart(event: DragEvent, item: ShopStockDto): void {
    event.dataTransfer?.setData('text/plain', `stock:${item.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'copy';
  }

  assignSelected(): void {
    if (!this.targetBinderId || this.selected().size === 0) return;
    const ids = [...this.selected()];
    this.saving.set(true);
    this.service.batchAssign(this.targetBinderId, ids).subscribe({
      next: () => {
        this.saving.set(false);
        this.selected.set(new Set());
        this.assigned.emit();
      },
      error: () => { this.saving.set(false); this.toast.error('Assignation impossible.'); },
    });
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('backdrop')) this.close.emit();
  }
}
