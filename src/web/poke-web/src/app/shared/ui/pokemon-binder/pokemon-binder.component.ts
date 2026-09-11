import {
  Component, EventEmitter, Output, ChangeDetectionStrategy, ElementRef, Injector,
  ViewChild, computed, effect, inject, input, signal, untracked, afterNextRender,
  HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Forme minimale attendue par le classeur — chaque écran (Bourse, Collection
 * admin/client…) mappe son propre DTO vers cette interface avant de la passer
 * en Input. `owned`/`favorite`/`priority` sont optionnels : les badges et le
 * niveau de gris associés ne s'affichent que si la valeur est fournie.
 */
export interface BinderItem {
  id: string;
  imageUrl: string | null;
  name: string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu
   *  serveur) — à privilégier sur `name` quand disponible. */
  displayName?: string | null;
  /** Image haute résolution pour la visualisation en grand — retombe sur
   *  `imageUrl` si absente (évite de charger du HD pour toutes les pochettes). */
  imageLargeUrl?: string | null;
  owned?: boolean;
  favorite?: boolean;
  priority?: boolean;
}

/**
 * Classeur à pochettes façon vrai album de cartes (double page, 3×3 par
 * page), réutilisable partout où on affiche une liste de cartes en thème
 * Pokémon. Un simple clic émet `itemClick` (laissé au parent — ex: toggle
 * "possédée" en Collection). Pour visualiser une carte en grand (tourbillon
 * + inclinaison 3D holographique), l'utilisateur glisse la pochette vers le
 * haut ("sort la carte de sa sleeve") ; un parent peut aussi déclencher la
 * visualisation programmatiquement via la méthode publique `viewItem(id)`
 * (utile depuis un menu contextuel "Visualiser").
 */
@Component({
  selector: 'app-pokemon-binder',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <ng-template #pocketsGrid let-page>
      @for (item of page; track item.id) {
        <button class="binder-pocket"
                type="button"
                [attr.data-pocket-id]="item.id"
                [class.binder-pocket--source]="isSelected(item)"
                [class.binder-pocket--notowned]="item.owned === false"
                (pointerdown)="onPocketPointerDown($event, item)"
                (pointermove)="onPocketPointerMove($event, item)"
                (pointerup)="onPocketPointerUp()"
                (pointercancel)="onPocketPointerCancel()"
                (click)="onPocketClick(item)"
                (contextmenu)="onPocketContextMenu($event, item)"
                [title]="item.displayName || item.name">
          <div class="binder-pocket__sheen" aria-hidden="true"></div>
          <div class="binder-pocket__img-wrap">
            @if (item.imageUrl) {
              <img [src]="item.imageUrl" [alt]="item.displayName || item.name" class="binder-pocket__img" loading="lazy" />
            } @else {
              <div class="binder-pocket__placeholder">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.35">
                  <rect x="3" y="3" width="18" height="18" rx="2"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
              </div>
            }
            @if (item.favorite) {
              <span class="pocket-badge pocket-badge--favorite" title="Favori">★</span>
            }
            @if (item.priority) {
              <span class="pocket-badge pocket-badge--priority" title="Prioritaire">!</span>
            }
            @if (item.owned === true) {
              <span class="pocket-badge pocket-badge--owned" title="Possédée"></span>
            }
          </div>
        </button>
      }
      @for (i of emptyPocketRange(page.length); track i) {
        <div class="binder-pocket binder-pocket--empty" aria-hidden="true"></div>
      }
    </ng-template>

    <div class="pkb-wrap">
      <div class="binder" [class.binder--dimmed]="!!selectedItem()">
        <div class="binder-spread"
             [class.binder-spread--out-next]="animState() === 'out' && flipDir() === 'next'"
             [class.binder-spread--out-prev]="animState() === 'out' && flipDir() === 'prev'"
             [class.binder-spread--in-next]="animState() === 'in' && flipDir() === 'next'"
             [class.binder-spread--in-prev]="animState() === 'in' && flipDir() === 'prev'">
          <div class="binder-page binder-page--left">
            <div class="binder-pockets">
              <ng-container [ngTemplateOutlet]="pocketsGrid" [ngTemplateOutletContext]="{ $implicit: leftPage() }" />
            </div>
          </div>
          <div class="binder-spine" aria-hidden="true">
            <div class="binder-spine__rings"></div>
          </div>
          <div class="binder-page binder-page--right">
            <div class="binder-pockets">
              <ng-container [ngTemplateOutlet]="pocketsGrid" [ngTemplateOutletContext]="{ $implicit: rightPage() }" />
            </div>
          </div>
        </div>

        <div class="binder-nav">
          <button class="binder-nav__btn" type="button"
                  [disabled]="spreadIndex() === 0"
                  (click)="flip('prev')" aria-label="Double page précédente du classeur">‹</button>
          <span class="binder-nav__page">Double page {{ spreadIndex() + 1 }} / {{ spreadCount() }}</span>
          <button class="binder-nav__btn" type="button"
                  [disabled]="spreadIndex() >= spreadCount() - 1"
                  (click)="flip('next')" aria-label="Double page suivante du classeur">›</button>
        </div>
      </div>

      @if (selectedItem(); as item) {
        <div class="pokemon-hero-wrap" (click)="closeView()">
          <div class="pokemon-hero" #heroCard
               (click)="$event.stopPropagation()"
               (mousemove)="onHeroTilt($event)"
               (mouseleave)="onHeroTiltLeave($event)">
            @if (item.imageLargeUrl ?? item.imageUrl; as heroImg) {
              <img [src]="heroImg" [alt]="item.displayName || item.name" class="pokemon-hero__img" />
            } @else {
              <div class="pokemon-hero__placeholder">{{ item.displayName || item.name }}</div>
            }
            <button class="pokemon-hero__close" type="button"
                    (click)="closeView(); $event.stopPropagation()" aria-label="Fermer la visualisation">×</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }

    .pkb-wrap {
      position: relative;
      display: flex;
      justify-content: center;
    }

    /* ── Classeur ── */
    .binder {
      position: relative;
      width: 100%;
      max-width: 1180px;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-4);
      padding: var(--space-6);
      border-radius: var(--radius-2xl);
      background:
        radial-gradient(circle at 15% 10%, rgba(255,255,255,0.08), transparent 45%),
        repeating-linear-gradient(135deg, rgba(255,255,255,0.03) 0px, rgba(255,255,255,0.03) 2px, transparent 2px, transparent 7px),
        linear-gradient(155deg, #37220f 0%, #24140a 45%, #150c07 100%);
      border: 1px solid rgba(0,0,0,0.55);
      box-shadow:
        0 32px 60px -12px rgba(0,0,0,0.55),
        0 12px 24px rgba(0,0,0,0.35),
        inset 0 1px 0 rgba(255,255,255,0.09),
        inset 0 0 0 1px rgba(0,0,0,0.35),
        inset 0 0 36px rgba(0,0,0,0.35);
      transition: opacity var(--duration-normal) var(--easing-standard);
    }
    .binder::before {
      content: '';
      position: absolute;
      inset: 11px;
      border: 1px dashed rgba(255,255,255,0.14);
      border-radius: calc(var(--radius-2xl) - 9px);
      pointer-events: none;
    }
    .binder::after {
      content: '';
      position: absolute;
      left: 8%;
      right: 8%;
      bottom: -16px;
      height: 28px;
      background: radial-gradient(ellipse at center, rgba(0,0,0,0.45), transparent 72%);
      filter: blur(5px);
      pointer-events: none;
      z-index: -1;
    }
    .binder--dimmed { opacity: 0.25; filter: saturate(0.7); pointer-events: none; }

    .binder-pocket--source .binder-pocket__img-wrap { opacity: 0; }

    .binder-spread { display: flex; align-items: stretch; justify-content: center; perspective: 2200px; }
    .binder-page { display: flex; align-items: center; }

    .binder-pockets {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 168px));
      gap: var(--space-3);
    }

    .binder-spine {
      width: 22px;
      flex-shrink: 0;
      align-self: stretch;
      margin: 0 var(--space-3);
      border-radius: 3px;
      background: linear-gradient(90deg, rgba(0,0,0,0.55), rgba(0,0,0,0.15) 25%, rgba(0,0,0,0.15) 75%, rgba(0,0,0,0.55));
      box-shadow: inset 0 0 14px rgba(0,0,0,0.65), var(--shadow-md);
      position: relative;
    }
    .binder-spine__rings {
      position: absolute;
      inset: 10px 7px;
      background-image: repeating-linear-gradient(180deg, rgba(255,255,255,0.18) 0, rgba(255,255,255,0.18) 3px, transparent 3px, transparent 24px);
      border-radius: 2px;
    }

    @keyframes binderFlipOutNext { from { transform: rotateY(0deg);   opacity: 1;   } to { transform: rotateY(-32deg); opacity: 0.2; } }
    @keyframes binderFlipInNext  { from { transform: rotateY(32deg);  opacity: 0.2; } to { transform: rotateY(0deg);   opacity: 1;   } }
    @keyframes binderFlipOutPrev { from { transform: rotateY(0deg);   opacity: 1;   } to { transform: rotateY(32deg);  opacity: 0.2; } }
    @keyframes binderFlipInPrev  { from { transform: rotateY(-32deg); opacity: 0.2; } to { transform: rotateY(0deg);   opacity: 1;   } }

    .binder-spread--out-next { transform-origin: left center;  animation: binderFlipOutNext 200ms ease-in forwards; }
    .binder-spread--in-next  { transform-origin: right center; animation: binderFlipInNext  200ms ease-out forwards; }
    .binder-spread--out-prev { transform-origin: right center; animation: binderFlipOutPrev 200ms ease-in forwards; }
    .binder-spread--in-prev  { transform-origin: left center;  animation: binderFlipInPrev  200ms ease-out forwards; }

    .binder-pocket {
      all: unset;
      position: relative;
      display: block;
      cursor: pointer;
      border-radius: 4px;
      border: 1px solid rgba(255,255,255,0.18);
      overflow: hidden;
      touch-action: none;
      transition: border-color var(--duration-fast) var(--easing-standard),
                  box-shadow var(--duration-fast) var(--easing-standard);
      &:hover, &:focus-visible {
        border-color: rgba(255,255,255,0.5);
        box-shadow: 0 0 0 2px rgba(255,255,255,0.14), var(--shadow-lg);
        outline: none;
      }
      &--empty {
        border-style: dashed;
        border-color: rgba(255,255,255,0.08);
        background: rgba(0,0,0,0.15);
        aspect-ratio: 63/88;
        cursor: default;
      }
    }
    .binder-pocket__sheen {
      position: absolute;
      inset: 0;
      background: linear-gradient(115deg, transparent 30%, rgba(255,255,255,0.18) 45%, transparent 60%);
      pointer-events: none;
      opacity: 0.8;
      z-index: 1;
    }
    .binder-pocket__img-wrap {
      position: relative;
      aspect-ratio: 63/88;
      overflow: hidden;
      background: var(--color-neutral-800);
    }
    .binder-pocket__img { width: 100%; height: 100%; object-fit: cover; display: block; transition: filter var(--duration-fast); }
    .binder-pocket--notowned .binder-pocket__img { filter: grayscale(100%) opacity(0.55); }
    .binder-pocket__placeholder { display: flex; align-items: center; justify-content: center; width: 100%; height: 100%; color: rgba(255,255,255,0.4); }

    .pocket-badge {
      position: absolute;
      font-size: 11px;
      line-height: 1;
      z-index: 2;
    }
    .pocket-badge--favorite {
      top: 4px; right: 4px;
      color: #f59e0b;
      text-shadow: 0 1px 2px rgba(0,0,0,0.6);
    }
    .pocket-badge--priority {
      top: 4px; left: 4px;
      background: #f97316; color: #fff;
      border-radius: var(--radius-sm);
      padding: 1px 4px;
      font-weight: 700;
    }
    .pocket-badge--owned {
      bottom: 4px; right: 4px;
      width: 9px; height: 9px; border-radius: 50%;
      background: var(--color-semantic-success);
      border: 1px solid rgba(0,0,0,0.3);
    }

    .binder-nav { display: flex; align-items: center; justify-content: center; gap: var(--space-4); padding-top: var(--space-2); }
    .binder-nav__btn {
      width: 34px; height: 34px; border-radius: var(--radius-full);
      border: 1px solid rgba(255,255,255,0.2);
      background: rgba(255,255,255,0.06);
      color: #fff; font-size: 18px; line-height: 1; cursor: pointer;
      display: flex; align-items: center; justify-content: center;
      transition: background var(--duration-fast), opacity var(--duration-fast);
      &:hover:not(:disabled) { background: rgba(255,255,255,0.16); }
      &:disabled { opacity: 0.3; cursor: not-allowed; }
    }
    .binder-nav__page { font-size: var(--font-size-xs); color: rgba(255,255,255,0.75); font-family: var(--font-family-mono); }

    /* ── Carte en premier plan (hero) ── */
    .pokemon-hero-wrap {
      position: absolute;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      perspective: 1600px;
    }
    .pokemon-hero {
      position: relative;
      width: min(62%, 300px);
      aspect-ratio: 63/88;
      border-radius: var(--radius-lg);
      cursor: pointer;
      transform-style: preserve-3d;
      transition: transform 120ms ease-out;
      box-shadow: 0 20px 50px rgba(0,0,0,0.55), 0 4px 14px rgba(0,0,0,0.4);
    }
    .pokemon-hero__img {
      width: 100%; height: 100%;
      object-fit: cover;
      display: block;
      border-radius: var(--radius-lg);
      backface-visibility: hidden;
    }
    .pokemon-hero__placeholder {
      width: 100%; height: 100%;
      display: flex; align-items: center; justify-content: center;
      text-align: center; padding: var(--space-3);
      background: var(--color-neutral-800);
      color: rgba(255,255,255,0.6);
      border-radius: var(--radius-lg);
      font-size: var(--font-size-sm);
    }
    .pokemon-hero__close {
      all: unset;
      position: absolute;
      top: -14px; right: -14px;
      width: 30px; height: 30px;
      border-radius: 50%;
      background: rgba(0,0,0,0.75);
      border: 1px solid rgba(255,255,255,0.25);
      color: #fff;
      font-size: 18px; line-height: 1;
      display: flex; align-items: center; justify-content: center;
      cursor: pointer;
      backdrop-filter: blur(2px);
      &:hover { background: rgba(0,0,0,0.9); }
    }
    @media (prefers-reduced-motion: reduce) {
      .pokemon-hero { transition: none; }
    }

    @media (prefers-reduced-motion: reduce) {
      .binder-spread { animation: none !important; }
      .binder-pocket { transition: none !important; }
      .pokemon-hero { transition: none !important; }
    }

    @media (max-width: 960px) {
      .binder-spread { flex-direction: column; align-items: center; gap: var(--space-4); }
      .binder-spine { display: none; }
      .binder-pockets { grid-template-columns: repeat(3, minmax(0, 140px)); }
      .pokemon-hero { width: min(70%, 220px); }
    }
  `],
})
export class PokemonBinderComponent {
  /** Cartes déjà mappées vers la forme générique attendue par le classeur. */
  readonly items = input.required<BinderItem[]>();

  /** Clic simple sur une pochette (pas de glissement) — laissé au parent. */
  @Output() itemClick = new EventEmitter<BinderItem>();
  /** Clic droit sur une pochette — laissé au parent (menu contextuel). */
  @Output() itemContextMenu = new EventEmitter<{ item: BinderItem; event: MouseEvent }>();

  private readonly injector = inject(Injector);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);

  readonly BINDER_PAGE_SIZE = 9;
  readonly spreadIndex = signal(0);
  readonly animState = signal<'idle' | 'out' | 'in'>('idle');
  readonly flipDir = signal<'next' | 'prev'>('next');

  readonly selectedId = signal<string | null>(null);
  readonly heroAnimating = signal(false);
  @ViewChild('heroCard') heroCardRef?: ElementRef<HTMLElement>;

  readonly pages = computed(() => {
    const items = this.items();
    if (items.length === 0) return [[] as BinderItem[]];
    const pages: BinderItem[][] = [];
    for (let i = 0; i < items.length; i += this.BINDER_PAGE_SIZE) {
      pages.push(items.slice(i, i + this.BINDER_PAGE_SIZE));
    }
    return pages;
  });

  readonly spreadCount = computed(() => Math.max(1, Math.ceil(this.pages().length / 2)));
  readonly leftPage = computed(() => this.pages()[this.spreadIndex() * 2] ?? []);
  readonly rightPage = computed(() => this.pages()[this.spreadIndex() * 2 + 1] ?? []);

  readonly selectedItem = computed(() => {
    const id = this.selectedId();
    return id ? (this.items().find(i => i.id === id) ?? null) : null;
  });

  private dragTrack: { pointerId: number; itemId: string; startY: number } | null = null;
  private justDraggedItemId: string | null = null;
  private readonly DRAG_UP_THRESHOLD = 40;
  private lastIdSignature = '';

  constructor() {
    // Revient à la première double page et ferme la visualisation seulement
    // quand la LISTE d'ids change réellement (nouveau set/filtre/page) — pas
    // à chaque mutation superficielle (ex: toggle "possédée" qui recrée le
    // tableau sans changer les ids), pour ne pas surprendre l'utilisateur en
    // train de feuilleter ou de visualiser une carte.
    effect(() => {
      const items = this.items();
      const sig = items.map(i => i.id).join('|');
      if (sig !== this.lastIdSignature) {
        this.lastIdSignature = sig;
        this.spreadIndex.set(0);
      }
      const selected = untracked(this.selectedId);
      if (selected && !items.some(i => i.id === selected)) {
        this.selectedId.set(null);
      }
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.selectedId()) this.closeView();
  }

  emptyPocketRange(filledCount: number): number[] {
    const remaining = Math.max(0, this.BINDER_PAGE_SIZE - filledCount);
    return Array.from({ length: remaining }, (_, i) => i);
  }

  isSelected(item: BinderItem): boolean {
    return this.selectedId() === item.id;
  }

  flip(direction: 'next' | 'prev'): void {
    const total = this.spreadCount();
    const target = direction === 'next'
      ? Math.min(this.spreadIndex() + 1, total - 1)
      : Math.max(this.spreadIndex() - 1, 0);
    if (target === this.spreadIndex()) return;

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reducedMotion) { this.spreadIndex.set(target); return; }

    this.flipDir.set(direction);
    this.animState.set('out');
    window.setTimeout(() => {
      this.spreadIndex.set(target);
      this.animState.set('in');
      window.setTimeout(() => this.animState.set('idle'), 220);
    }, 200);
  }

  // ── Interaction pochette : clic simple (laissé au parent) vs glisser pour sortir la carte ──
  onPocketPointerDown(event: PointerEvent, item: BinderItem): void {
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    this.dragTrack = { pointerId: event.pointerId, itemId: item.id, startY: event.clientY };
  }

  onPocketPointerMove(event: PointerEvent, item: BinderItem): void {
    const t = this.dragTrack;
    if (!t || t.pointerId !== event.pointerId || t.itemId !== item.id) return;
    if (t.startY - event.clientY > this.DRAG_UP_THRESHOLD) {
      this.dragTrack = null;
      this.justDraggedItemId = item.id;
      this.viewItem(item.id);
    }
  }

  onPocketPointerUp(): void { this.dragTrack = null; }
  onPocketPointerCancel(): void { this.dragTrack = null; }

  onPocketClick(item: BinderItem): void {
    // Le glissement vers le haut a déjà déclenché la visualisation via
    // pointermove : on avale le click de suivi pour ne pas aussi remonter
    // itemClick (qui, côté Collection, bascule "possédée").
    if (this.justDraggedItemId === item.id) {
      this.justDraggedItemId = null;
      return;
    }
    this.itemClick.emit(item);
  }

  onPocketContextMenu(event: MouseEvent, item: BinderItem): void {
    this.itemContextMenu.emit({ item, event });
  }

  // ── Visualisation (tourbillon + inclinaison 3D holographique) ──
  /** Ouvre la visualisation grand format d'une carte — appelable aussi bien
   *  depuis le glisser-sortir (pointermove) que depuis un menu contextuel
   *  parent ("Visualiser"). */
  viewItem(id: string): void {
    const item = this.items().find(i => i.id === id);
    if (!item) return;
    const startRect = this.pocketEl(id)?.getBoundingClientRect() ?? null;
    this.selectedId.set(id);
    if (!startRect) return;

    afterNextRender(() => {
      const hero = this.heroCardRef?.nativeElement;
      if (hero) this.playHeroSpin(hero, startRect, 'enter');
    }, { injector: this.injector });
  }

  closeView(): void {
    const id = this.selectedId();
    if (!id) return;
    const hero = this.heroCardRef?.nativeElement;
    const pocket = this.pocketEl(id);
    if (!hero || !pocket) { this.selectedId.set(null); return; }
    const endRect = pocket.getBoundingClientRect();
    this.playHeroSpin(hero, endRect, 'exit').finally(() => this.selectedId.set(null));
  }

  private playHeroSpin(hero: HTMLElement, pocketRect: DOMRect, mode: 'enter' | 'exit'): Promise<void> {
    hero.style.transform = '';
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reducedMotion) return Promise.resolve();

    const heroRect = hero.getBoundingClientRect();
    const dx = pocketRect.left + pocketRect.width / 2 - (heroRect.left + heroRect.width / 2);
    const dy = pocketRect.top + pocketRect.height / 2 - (heroRect.top + heroRect.height / 2);
    const scale = Math.max(0.05, pocketRect.width / heroRect.width);
    const midScale = (scale + 1) / 2;

    const enterFrames: Keyframe[] = [
      { transform: `translate(${dx}px, ${dy}px) scale(${scale}) rotateY(0deg)`, opacity: 0.5, offset: 0 },
      { transform: `translate(${dx * 0.35}px, ${dy * 0.35}px) scale(${midScale}) rotateY(180deg)`, opacity: 1, offset: 0.55 },
      { transform: 'translate(0px, 0px) scale(1) rotateY(360deg)', opacity: 1, offset: 1 },
    ];
    const exitFrames: Keyframe[] = [
      { transform: 'translate(0px, 0px) scale(1) rotateY(0deg)', opacity: 1, offset: 0 },
      { transform: `translate(${dx * 0.35}px, ${dy * 0.35}px) scale(${midScale}) rotateY(-180deg)`, opacity: 1, offset: 0.45 },
      { transform: `translate(${dx}px, ${dy}px) scale(${scale}) rotateY(-360deg)`, opacity: 0.35, offset: 1 },
    ];

    this.heroAnimating.set(true);
    const anim = hero.animate(mode === 'enter' ? enterFrames : exitFrames, {
      duration: mode === 'enter' ? 680 : 560,
      easing: mode === 'enter' ? 'cubic-bezier(0.22, 0.68, 0.24, 1)' : 'cubic-bezier(0.55, 0, 0.75, 0.1)',
      fill: 'both',
    });

    return anim.finished
      .then(
        () => undefined,
        () => undefined,
      )
      .finally(() => {
        this.heroAnimating.set(false);
        anim.cancel();
      });
  }

  onHeroTilt(event: MouseEvent): void {
    if (this.heroAnimating()) return;
    const el = event.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    const px = (event.clientX - rect.left) / rect.width - 0.5;
    const py = (event.clientY - rect.top) / rect.height - 0.5;
    const maxTilt = 14;
    el.style.transform = `rotateX(${(-py * maxTilt).toFixed(2)}deg) rotateY(${(px * maxTilt).toFixed(2)}deg) scale(1.03)`;
  }

  onHeroTiltLeave(event: MouseEvent): void {
    if (this.heroAnimating()) return;
    (event.currentTarget as HTMLElement).style.transform = '';
  }

  private pocketEl(id: string): HTMLElement | null {
    return this.host.nativeElement.querySelector(`[data-pocket-id="${CSS.escape(id)}"]`) as HTMLElement | null;
  }
}
