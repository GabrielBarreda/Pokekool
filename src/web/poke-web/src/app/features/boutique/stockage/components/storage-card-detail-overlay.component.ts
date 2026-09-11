import { Component, inject, input, output, signal, effect } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { StorageService } from '../storage.service';
import { ToastService } from '@shared/services/toast.service';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { StorageBinderCardDto, StorageCardDetailDto } from '../storage.models';

/**
 * Side-view détail carte (comptoir) : image, set, condition/grade, prix estimé et
 * valeurs récentes dans le même état/grade. Panneau qui glisse depuis la droite.
 */
@Component({
  selector: 'app-storage-card-detail-overlay',
  standalone: true,
  imports: [DatePipe, DecimalPipe, SpinnerComponent],
  template: `
    <div class="scrim" (click)="close.emit()"></div>
    <aside class="panel">
      <header class="panel__head">
        <button class="panel__close" (click)="close.emit()" title="Fermer">✕</button>
        <span class="panel__title">Détail carte</span>
      </header>

      @if (loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      } @else if (detail(); as d) {
        <div class="body">
          <div class="hero">
            @if (d.imageLarge || d.imageSmall) {
              <img class="hero__img" [src]="d.imageLarge || d.imageSmall" [alt]="d.displayName || d.name" />
            }
          </div>
          <h3 class="name">{{ d.displayName || d.name }}</h3>
          @if (d.setName) { <div class="set">{{ d.setName }}</div> }

          <div class="badges">
            @if (d.gradingCompany && d.gradeValue != null) {
              <span class="badge badge--grade">{{ d.gradingCompany }} {{ d.gradeValue }}</span>
            } @else if (d.condition) {
              <span class="badge badge--cond">{{ d.condition }}</span>
            }
            <span class="badge badge--qty">Dispo : {{ d.quantityAvailable }}</span>
          </div>

          <div class="price">
            <span class="price__label">Prix estimé</span>
            @if (d.estimatedPrice != null) {
              <span class="price__val">{{ d.estimatedPrice | number:'1.2-2' }} {{ d.estimatedCurrency }}</span>
            } @else {
              <span class="price__na">indisponible</span>
            }
          </div>

          <div class="sales">
            <div class="sales__title">
              Valeurs récentes {{ d.gradingCompany ? '(même grade)' : '(marché)' }}
            </div>
            @if (d.recentPrices.length === 0) {
              <div class="sales__empty">Aucune donnée de marché pour cette carte / ce grade.</div>
            } @else {
              <ul class="sales__list">
                @for (p of d.recentPrices; track $index) {
                  <li class="sale">
                    <span class="sale__src">{{ p.source }}</span>
                    <span class="sale__date">{{ p.fetchedAt | date:'dd/MM/yy' }}</span>
                    <span class="sale__price">{{ p.price | number:'1.2-2' }} {{ p.currency }}</span>
                  </li>
                }
              </ul>
            }
          </div>
        </div>
      } @else {
        <div class="center center--na">Détail indisponible pour cet article.</div>
      }
    </aside>
  `,
  styles: [`
    :host { position: absolute; inset: 0; z-index: 40; }
    .scrim { position: absolute; inset: 0; background: rgba(0,0,0,.45); animation: fade .2s both; }
    @keyframes fade { from { opacity: 0; } to { opacity: 1; } }
    .panel { position: absolute; top: 0; right: 0; bottom: 0; width: min(380px, 92%);
      background: var(--color-surface-base); border-left: 1px solid var(--color-border-default);
      box-shadow: var(--shadow-xl); display: flex; flex-direction: column;
      animation: slide .28s cubic-bezier(.2,.7,.2,1) both; }
    @keyframes slide { from { transform: translateX(100%); } to { transform: translateX(0); } }
    .panel__head { display: flex; align-items: center; gap: .6rem; padding: .6rem .8rem; border-bottom: 1px solid var(--color-border-default); }
    .panel__title { font-weight: 600; }
    .panel__close { margin-left: auto; border: none; background: none; font-size: 1rem; cursor: pointer; color: var(--color-text-secondary); }
    .center { flex: 1; display: flex; align-items: center; justify-content: center; color: var(--color-text-muted); }
    .body { overflow: auto; padding: 1rem; display: flex; flex-direction: column; gap: .6rem; }
    .hero { display: flex; justify-content: center; }
    .hero__img { max-height: 300px; border-radius: 10px; box-shadow: var(--shadow-lg); }
    .name { margin: .2rem 0 0; font-size: 1.1rem; }
    .set { color: var(--color-text-muted); font-size: .85rem; }
    .badges { display: flex; flex-wrap: wrap; gap: .4rem; margin: .3rem 0; }
    .badge { font-size: .78rem; padding: .2rem .55rem; border-radius: 999px; }
    .badge--grade { background: #fff3cd; color: #7a5b00; font-weight: 600; }
    .badge--cond { background: #e7f0ff; color: #234; }
    .badge--qty { background: var(--color-surface-raised); color: var(--color-text-secondary); }
    .price { display: flex; align-items: baseline; justify-content: space-between; padding: .5rem .7rem; background: var(--color-surface-raised); border-radius: 10px; }
    .price__label { font-size: .8rem; color: var(--color-text-secondary); }
    .price__val { font-size: 1.3rem; font-weight: 700; color: var(--color-brand-primary); }
    .price__na { color: var(--color-text-muted); }
    .sales__title { font-size: .82rem; color: var(--color-text-secondary); margin-bottom: .3rem; }
    .sales__empty { font-size: .82rem; color: var(--color-text-muted); }
    .sales__list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: .25rem; }
    .sale { display: grid; grid-template-columns: 1fr auto auto; gap: .6rem; align-items: center;
      padding: .35rem .5rem; background: var(--color-surface-raised); border-radius: 7px; font-size: .82rem; }
    .sale__src { text-transform: capitalize; }
    .sale__date { color: var(--color-text-muted); }
    .sale__price { font-weight: 600; }
  `],
})
export class StorageCardDetailOverlayComponent {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly card = input.required<StorageBinderCardDto>();
  readonly close = output<void>();

  readonly loading = signal(true);
  readonly detail = signal<StorageCardDetailDto | null>(null);

  constructor() {
    effect(() => {
      const c = this.card();
      if (c) this.load(c.ownerType, c.ownerId);
    });
  }

  private load(ownerType: string, ownerId: string): void {
    this.loading.set(true);
    this.service.getCardDetail(ownerType, ownerId).subscribe({
      next: d => { this.detail.set(d); this.loading.set(false); },
      error: () => { this.detail.set(null); this.loading.set(false); this.toast.error('Détail carte indisponible.'); },
    });
  }
}
