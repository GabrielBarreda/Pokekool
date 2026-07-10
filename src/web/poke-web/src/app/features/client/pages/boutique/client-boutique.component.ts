import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClientService } from '../../client.service';
import { ShopProductDto, ShopProductFilters } from '../../client.models';
import { ToastService } from '@shared/services/toast.service';
import { CARD_CONDITIONS, conditionLabel } from '@shared/constants/card-condition.labels';
import { ThemeStore } from '@core/theme/theme.store';

interface TypeMeta { short: string; icon: string; cls: string; }

const TYPE_META: Record<string, TypeMeta> = {
  CardRaw:    { short: 'Brute',  icon: '🃏', cls: 'raw' },
  CardGraded: { short: 'Gradée', icon: '🏅', cls: 'graded' },
  SealedSet:  { short: 'Set',    icon: '📦', cls: 'sealed-set' },
  SealedUpc:  { short: 'Scellé', icon: '🎁', cls: 'sealed-upc' },
};

@Component({
  selector: 'app-client-boutique',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="boutique-page" [class.plv]="themeStore.isPokemon()">
      <header class="page-head">
        <div>
          <h1 class="page-head__title">Boutique</h1>
          <p class="page-head__sub">Articles disponibles en boutique. Aucun paiement en ligne — transaction en main propre.</p>
        </div>
      </header>

      <section class="filters">
        <input class="input" type="search" placeholder="Rechercher…" [ngModel]="search()" (ngModelChange)="onSearch($event)" />
        <select class="input input--sm" [ngModel]="type()" (ngModelChange)="onType($event)">
          <option value="">Tous types</option>
          <option value="CardRaw">Carte brute</option>
          <option value="CardGraded">Carte gradée</option>
          <option value="SealedSet">Scellé (set)</option>
          <option value="SealedUpc">Scellé (UPC)</option>
        </select>
        <input class="input input--sm" type="number" min="0" placeholder="Prix max €" [ngModel]="priceMax()" (ngModelChange)="onPriceMax($event)" />
      </section>

      @if (loading()) {
        <p class="muted">Chargement…</p>
      } @else if (products().length === 0) {
        <p class="muted">Aucun article disponible pour le moment.</p>
      } @else {
        @if (themeStore.isPokemon()) {

          <!-- ═══════════════════════════════════════════════
               PLV POKÉMON — réplique du présentoir physique
               Zénith Suprême : header diamond + niches blanches
               + fasciae losange + bordures arc-en-ciel
               ═══════════════════════════════════════════════ -->
          <div class="plv-wrap">

            <!-- Bandes arc-en-ciel verticales (côtés du stand) -->
            <div class="plv-side plv-side--left"  aria-hidden="true"></div>
            <div class="plv-side plv-side--right" aria-hidden="true"></div>

            <div class="plv-stand">

              <!-- ── HEADER ──────────────────────────────────────── -->
              <div class="plv-header">
                <div class="plv-header__bg" aria-hidden="true"></div>
                <div class="plv-header__content">
                  <div class="plv-header__pika" aria-hidden="true">
                    <span class="plv-pika-emoji">🟡</span>
                  </div>
                  <div class="plv-header__text">
                    <span class="plv-header__brand">Pokémon</span>
                    <span class="plv-header__tagline">Cartes à jouer et à collectionner</span>
                    <span class="plv-header__set">Boutique · Présentoir</span>
                  </div>
                </div>
              </div>

              <!-- ── ÉTAGÈRES ────────────────────────────────────── -->
              @for (row of productRows(); track $index) {
                <div class="plv-shelf-unit">

                  <!-- Niche blanche — espace de présentation -->
                  <div class="plv-niche">
                    <!-- Gradient de profondeur sur le fond de niche -->
                    <div class="plv-niche__depth" aria-hidden="true"></div>

                    <!-- Grille de produits -->
                    <div class="plv-niche__grid">
                      @for (p of row; track p.id) {
                        <button type="button" class="plv-item" (click)="openModal(p)">

                          <!-- Image — ratio portrait 2.5/3.5 comme la collection -->
                          <div class="plv-item__card">
                            @if (imageOf(p)) {
                              <img class="plv-item__img" [src]="imageOf(p)" [alt]="titleOf(p)" loading="lazy" />
                            } @else {
                              <div class="plv-item__ph">
                                <span>{{ typeIcon(p.type) }}</span>
                              </div>
                            }
                            <!-- Reflet holographique -->
                            <span class="plv-item__sheen" aria-hidden="true"></span>
                          </div>

                          <!-- Étiquette prix -->
                          <div class="plv-item__label">
                            <span class="plv-item__name">{{ titleOf(p) }}</span>
                            <span class="plv-item__sub">
                              @if (p.gradingCompany) { {{ p.gradingCompany }} {{ p.gradeValue ?? '' }}
                              } @else if (p.condition) { {{ conditionLabel(p.condition) }}
                              } @else { {{ typeShort(p.type) }} }
                              @if (p.quantity > 1) { &nbsp;×{{ p.quantity }} }
                            </span>
                            <span class="plv-item__price">{{ formatMoney(p.price) }} {{ p.currency }}</span>
                          </div>

                        </button>
                      }
                    </div>
                  </div>

                  <!-- Fascia losange (bord avant de chaque étagère) -->
                  <div class="plv-fascia">
                    <span class="plv-fascia__ball" aria-hidden="true"></span>
                    <span class="plv-fascia__text">Pokémon · Boutique</span>
                    <span class="plv-fascia__ball" aria-hidden="true"></span>
                  </div>

                </div>
              }

              <!-- Pied du stand -->
              <div class="plv-footer">
                <span>Transaction en main propre · Pas de paiement en ligne</span>
              </div>

            </div><!-- /plv-stand -->
          </div><!-- /plv-wrap -->

        } @else {
          <div class="grid">
            @for (p of products(); track p.id) {
              <article class="product">
                <div class="product__img">
                  @if (imageOf(p)) { <img [src]="imageOf(p)" [alt]="titleOf(p)" loading="lazy" /> }
                  @else { <div class="product__ph">{{ titleOf(p) }}</div> }
                </div>
                <div class="product__body">
                  <h3 class="product__title">{{ titleOf(p) }}</h3>
                  <p class="product__meta">
                    {{ typeLabel(p.type) }}
                    @if (p.gradingCompany) { · {{ p.gradingCompany }} {{ p.gradeValue }} }
                    @else if (p.condition) { · {{ conditionLabel(p.condition) }} }
                  </p>
                  <p class="product__price">{{ formatMoney(p.price) }} {{ p.currency }}</p>
                  <button type="button" class="btn btn--primary" (click)="openModal(p)">Je suis intéressé</button>
                </div>
              </article>
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

      <!-- Modale demande d'achat -->
      @if (selected(); as prod) {
        <div class="overlay" (click)="closeModal()">
          <div class="modal" (click)="$event.stopPropagation()">
            <h2 class="modal__title">Demande d'achat</h2>
            <p class="modal__sub">{{ titleOf(prod) }} — {{ formatMoney(prod.price) }} {{ prod.currency }}</p>
            <p class="modal__note">Vos nom et email sont pris depuis votre compte. La boutique vous recontactera.</p>
            <textarea class="textarea" rows="4" placeholder="Message (optionnel)" [(ngModel)]="message"></textarea>
            <div class="modal__actions">
              <button type="button" class="btn" (click)="closeModal()">Annuler</button>
              <button type="button" class="btn btn--primary" [disabled]="submitting()" (click)="submit(prod)">Envoyer ma demande</button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-head { display: flex; align-items: center; gap: var(--space-3); margin-bottom: var(--space-5); }
    .page-head__title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); margin: 0; }
    .page-head__sub { color: var(--color-text-muted); margin: var(--space-1) 0 0; }

    .filters { display: flex; flex-wrap: wrap; gap: var(--space-3); margin-bottom: var(--space-4); }
    .input { height: 38px; padding: 0 var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); color: var(--color-text-primary); font-size: var(--font-size-sm); }
    .input:not(.input--sm) { flex: 1; min-width: 200px; }
    .input--sm { width: 150px; }

    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: var(--space-4); }
    .product { border: 1px solid var(--color-border-subtle); border-radius: var(--radius-lg); background: var(--color-surface-base); overflow: hidden; display: flex; flex-direction: column; }
    .product__img { aspect-ratio: 4/3; background: var(--color-surface-sunken); display: flex; align-items: center; justify-content: center; }
    .product__img img { width: 100%; height: 100%; object-fit: contain; }
    .product__ph { font-size: var(--font-size-sm); color: var(--color-text-muted); padding: var(--space-3); text-align: center; }
    .product__body { padding: var(--space-3); display: flex; flex-direction: column; gap: var(--space-2); flex: 1; }
    .product__title { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); margin: 0; }
    .product__meta { font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 0; }
    .product__price { font-size: var(--font-size-lg); font-weight: var(--font-weight-bold); color: var(--color-brand-primary); margin: 0 0 var(--space-1); }

    .btn { padding: var(--space-2) var(--space-4); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); cursor: pointer; font-size: var(--font-size-sm); }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn--primary { background: var(--color-brand-primary); color: #fff; border-color: var(--color-brand-primary); margin-top: auto; }

    .pager { display: flex; align-items: center; justify-content: center; gap: var(--space-4); margin-top: var(--space-5); font-size: var(--font-size-sm); }
    .muted { color: var(--color-text-muted); }

    .overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 100; padding: var(--space-4); }
    .modal { background: var(--color-surface-base); border-radius: var(--radius-xl); padding: var(--space-6); width: 100%; max-width: 440px; display: flex; flex-direction: column; gap: var(--space-3); box-shadow: var(--shadow-lg); }
    .modal__title { margin: 0; font-size: var(--font-size-xl); }
    .modal__sub { margin: 0; font-weight: var(--font-weight-semibold); }
    .modal__note { margin: 0; font-size: var(--font-size-sm); color: var(--color-text-muted); }
    .textarea { width: 100%; padding: var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); font-family: inherit; font-size: var(--font-size-sm); resize: vertical; background: var(--color-surface-base); color: var(--color-text-primary); }
    .modal__actions { display: flex; justify-content: flex-end; gap: var(--space-3); }

    /* ════════════════════════════════════════════════════════════
       PLV POKÉMON — Zénith Suprême
       Réplique du présentoir physique :
         • bandes arc-en-ciel verticales
         • header diamond texture
         • niches BLANCHES avec profondeur
         • fasciae losange charcoal
       ════════════════════════════════════════════════════════════ */

    /* ── Outer wrapper — positionne les bandes latérales ── */
    .plv-wrap {
      position: relative;
      max-width: 700px;
      margin: 0 auto;
      padding: 0 14px;        /* espace pour les bandes */
    }

    /* ── Bandes arc-en-ciel verticales ── */
    .plv-side {
      position: absolute;
      top: 0; bottom: 0;
      width: 12px;
      border-radius: 4px;
      background: linear-gradient(180deg,
        #ff0033  0%,
        #ff4400  7%,
        #ff7700 14%,
        #ffaa00 21%,
        #ffdd00 28%,
        #aaee00 35%,
        #00dd66 42%,
        #00cccc 49%,
        #0099ff 56%,
        #3366ff 63%,
        #6633ff 70%,
        #aa00ff 77%,
        #ee00bb 84%,
        #ff0066 92%,
        #ff0033 100%
      );
      z-index: 2;
    }
    .plv-side--left  { left: 0; }
    .plv-side--right { right: 0; }

    /* ── Stand principal ── */
    .plv-stand {
      display: flex;
      flex-direction: column;
      background: #111118;
      border-radius: 6px;
      overflow: hidden;
      box-shadow:
        0 0 0 1px rgba(255,255,255,0.08),
        0 16px 48px rgba(0,0,0,0.7);
    }

    /* ── Header — texture losange + branding ── */
    .plv-header {
      position: relative;
      overflow: hidden;
      padding: 20px 24px 22px;
      border-bottom: 2px solid rgba(255,203,5,0.3);
    }
    /* Background losange */
    .plv-header__bg {
      position: absolute; inset: 0;
      background-color: #111122;
      background-image:
        repeating-linear-gradient(45deg,
          rgba(255,255,255,0.04) 0,
          rgba(255,255,255,0.04) 1px,
          transparent 1px,
          transparent 50%),
        repeating-linear-gradient(-45deg,
          rgba(255,255,255,0.04) 0,
          rgba(255,255,255,0.04) 1px,
          transparent 1px,
          transparent 50%);
      background-size: 14px 14px;
      pointer-events: none;
    }
    /* Reflet diagonal sur le header */
    .plv-header__bg::after {
      content: '';
      position: absolute; inset: 0;
      background: linear-gradient(115deg,
        transparent 30%,
        rgba(255,255,255,0.04) 50%,
        transparent 70%);
    }
    .plv-header__content {
      position: relative; z-index: 1;
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .plv-header__pika {
      font-size: 48px;
      line-height: 1;
      filter: drop-shadow(0 0 12px rgba(255,220,0,0.5));
      flex-shrink: 0;
    }
    .plv-pika-emoji { display: block; }
    .plv-header__text {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .plv-header__brand {
      font-size: 26px;
      font-weight: 900;
      color: #ffcb05;
      letter-spacing: 3px;
      text-transform: uppercase;
      text-shadow:
        2px 2px 0 rgba(0,0,0,0.6),
        0 0 16px rgba(255,203,5,0.35);
      line-height: 1;
    }
    .plv-header__tagline {
      font-size: 9px;
      color: rgba(255,255,255,0.45);
      letter-spacing: 2px;
      text-transform: uppercase;
    }
    .plv-header__set {
      font-size: 11px;
      font-weight: 700;
      color: rgba(255,255,255,0.6);
      letter-spacing: 1px;
      text-transform: uppercase;
      margin-top: 2px;
    }

    /* ══════════════════════════════════════
       ÉTAGÈRE — niche blanche + fascia
       ══════════════════════════════════════ */
    .plv-shelf-unit {
      display: flex;
      flex-direction: column;
    }

    /* ── Niche blanche (intérieur de l'étagère) ── */
    .plv-niche {
      position: relative;
      background: #f2f0ea;      /* blanc légèrement chaud = impression de profondeur */
      padding: 20px 12px 4px;
      overflow: hidden;
    }
    /* Gradient de profondeur — haut plus sombre, fond lumineux */
    .plv-niche__depth {
      position: absolute;
      inset: 0;
      background: linear-gradient(180deg,
        rgba(0,0,0,0.18) 0%,
        rgba(0,0,0,0.04) 30%,
        rgba(255,255,255,0.04) 70%,
        rgba(0,0,0,0.12) 100%);
      pointer-events: none;
      z-index: 0;
    }
    /* Ombre portée en bas de la niche = sol réel */
    .plv-niche::after {
      content: '';
      position: absolute;
      bottom: 0; left: 0; right: 0;
      height: 14px;
      background: linear-gradient(180deg,
        transparent 0%,
        rgba(0,0,0,0.15) 100%);
      pointer-events: none;
      z-index: 1;
    }

    /* ── Grille produits dans la niche ── */
    .plv-niche__grid {
      position: relative; z-index: 2;
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 0 8px;
      padding-bottom: 10px;
    }

    /* ── Item produit ── */
    .plv-item {
      all: unset;
      display: flex;
      flex-direction: column;
      align-items: center;
      cursor: pointer;
      position: relative;
    }

    /* ── Visuel carte — ratio 2.5/3.5, lévitation ── */
    .plv-item__card {
      position: relative;
      aspect-ratio: 2.5 / 3.5;
      width: 100%;
      border-radius: 8px;
      overflow: hidden;
      background: #07111f;
      border: 1px solid rgba(255,255,255,0.2);
      /* Ombre réaliste sur sol blanc — simule la lévitation */
      box-shadow:
        0 10px 20px rgba(0,0,0,0.35),
        0 4px 8px rgba(0,0,0,0.25),
        0 2px 3px rgba(0,0,0,0.2);
      transform: translateY(-6px);
      transition:
        transform 0.22s ease,
        box-shadow 0.22s ease;
    }
    .plv-item:hover .plv-item__card,
    .plv-item:focus-visible .plv-item__card {
      transform: translateY(-14px) scale(1.04);
      box-shadow:
        0 22px 36px rgba(0,0,0,0.45),
        0 8px 16px rgba(0,0,0,0.3),
        0 0 0 1.5px rgba(255,203,5,0.7);
      border-color: rgba(255,203,5,0.6);
    }
    .plv-item__img {
      width: 100%; height: 100%;
      object-fit: cover;
      display: block;
      transition: transform 0.25s ease;
    }
    .plv-item:hover .plv-item__img { transform: scale(1.04); }
    .plv-item__ph {
      width: 100%; height: 100%;
      display: flex; align-items: center; justify-content: center;
      background: linear-gradient(135deg, #0d1a3a, #07111f);
      font-size: 28px;
      color: rgba(255,203,5,0.4);
    }
    /* Reflet holographique */
    .plv-item__sheen {
      position: absolute; inset: 0;
      background: linear-gradient(115deg,
        transparent 30%,
        rgba(255,255,255,0) 44%,
        rgba(255,255,255,0.3) 50%,
        rgba(255,255,255,0) 56%,
        transparent 70%);
      opacity: 0;
      transition: opacity 0.25s ease;
      pointer-events: none;
    }
    .plv-item:hover .plv-item__sheen { opacity: 1; }

    /* ── Étiquette prix (sous la carte, crème carton) ── */
    .plv-item__label {
      width: 100%;
      background: #fefcf0;
      border: 1px solid rgba(0,0,0,0.12);
      border-top: 2px solid #ffcb05;
      border-radius: 0 0 4px 4px;
      padding: 3px 5px 5px;
      display: flex;
      flex-direction: column;
      gap: 1px;
      box-shadow: 0 3px 8px rgba(0,0,0,0.2);
      margin-top: -6px;           /* colle sous la carte flottante */
      position: relative; z-index: 1;
    }
    .plv-item__name {
      font-size: 9px; font-weight: 700; color: #1a1a1a;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
      line-height: 1.2;
    }
    .plv-item__sub {
      font-size: 8px; color: #666;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .plv-item__price {
      font-size: 12px; font-weight: 900; color: #c0392b;
      letter-spacing: -0.02em; margin-top: 2px;
    }

    /* ══════════════════════════════════════
       FASCIA — bord avant losange + texte
       ══════════════════════════════════════ */
    .plv-fascia {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 10px;
      height: 32px;
      padding: 0 16px;
      /* Fond losange charcoal */
      background-color: #1e1e26;
      background-image:
        repeating-linear-gradient(45deg,
          rgba(255,255,255,0.035) 0,
          rgba(255,255,255,0.035) 1px,
          transparent 1px,
          transparent 50%),
        repeating-linear-gradient(-45deg,
          rgba(255,255,255,0.035) 0,
          rgba(255,255,255,0.035) 1px,
          transparent 1px,
          transparent 50%);
      background-size: 10px 10px;
      border-top: 2px solid rgba(255,203,5,0.25);
      border-bottom: 1px solid rgba(0,0,0,0.4);
      box-shadow: inset 0 1px 0 rgba(255,255,255,0.06);
    }
    .plv-fascia__text {
      font-size: 9px;
      font-weight: 700;
      color: rgba(255,255,255,0.55);
      letter-spacing: 2.5px;
      text-transform: uppercase;
    }
    .plv-fascia__ball {
      width: 8px; height: 8px; flex-shrink: 0; border-radius: 50%;
      background:
        radial-gradient(circle at 50% 50%, #fff 0 16%, transparent 17%),
        linear-gradient(#ee1515 0 48%, #333 48% 52%, #fff 52% 100%);
      opacity: 0.45;
    }

    /* ── Pied du stand ── */
    .plv-footer {
      padding: 8px 16px;
      background: #0c0c14;
      border-top: 1px solid rgba(255,203,5,0.15);
      font-size: 8px;
      color: rgba(255,255,255,0.25);
      text-align: center;
      letter-spacing: 1px;
      text-transform: uppercase;
    }

    /* ── Responsive ── */
    @media (max-width: 600px) {
      .plv-niche__grid { grid-template-columns: repeat(2, 1fr); }
      .plv-header__brand { font-size: 20px; letter-spacing: 2px; }
      .plv-header__pika { font-size: 36px; }
      .plv-wrap { padding: 0 10px; }
      .plv-side { width: 9px; }
    }
    @media (max-width: 360px) {
      .plv-niche__grid { grid-template-columns: repeat(2, 1fr); gap: 0 4px; }
    }
  `],
})
export class ClientBoutiqueComponent implements OnInit {
  readonly themeStore = inject(ThemeStore);
  readonly conditions = CARD_CONDITIONS;
  readonly conditionLabel = conditionLabel;
  private readonly service = inject(ClientService);
  private readonly toast = inject(ToastService);

  readonly products = signal<ShopProductDto[]>([]);
  readonly loading = signal(false);
  readonly page = signal(1);
  readonly totalPages = signal(1);

  readonly search = signal('');
  readonly type = signal('');
  readonly priceMax = signal<number | null>(null);

  readonly selected = signal<ShopProductDto | null>(null);
  readonly submitting = signal(false);
  message = '';

  /** Produits chunkés par 4 pour l'affichage en étagères (thème Pokémon). */
  readonly productRows = computed(() => this.chunk(this.products(), 4));

  private searchDebounce?: ReturnType<typeof setTimeout>;

  ngOnInit(): void { this.load(); }

  formatMoney(v: number): string {
    return (v ?? 0).toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
  imageOf(p: ShopProductDto): string | null { return p.photoUrl || p.cardImage || null; }
  titleOf(p: ShopProductDto): string { return p.productName || p.cardName || 'Article'; }
  typeLabel(t: string): string {
    return ({ CardRaw: 'Carte brute', CardGraded: 'Carte gradée', SealedSet: 'Scellé', SealedUpc: 'Scellé UPC' } as Record<string, string>)[t] ?? t;
  }
  typeShort(t: string): string { return TYPE_META[t]?.short ?? t; }
  typeIcon(t: string): string { return TYPE_META[t]?.icon ?? '🃏'; }
  typeColorClass(t: string): string { return TYPE_META[t]?.cls ?? 'raw'; }

  private chunk<T>(arr: T[], size: number): T[][] {
    const out: T[][] = [];
    for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
    return out;
  }

  private load(): void {
    this.loading.set(true);
    const filters: ShopProductFilters = {
      search: this.search() || undefined,
      type: this.type() || undefined,
      priceMax: this.priceMax() ?? undefined,
      page: this.page(),
      pageSize: 24,
    };
    this.service.getProducts(filters).subscribe({
      next: res => { this.products.set(res.items); this.totalPages.set(res.totalPages); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  onSearch(v: string): void {
    this.search.set(v);
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => { this.page.set(1); this.load(); }, 300);
  }
  onType(v: string): void { this.type.set(v); this.page.set(1); this.load(); }
  onPriceMax(v: number | null): void { this.priceMax.set(v); this.page.set(1); this.load(); }
  goTo(p: number): void { this.page.set(p); this.load(); }

  openModal(p: ShopProductDto): void { this.message = ''; this.selected.set(p); }
  closeModal(): void { this.selected.set(null); }

  submit(p: ShopProductDto): void {
    this.submitting.set(true);
    this.service.requestPurchase(p.id, this.message.trim() || null).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closeModal();
        this.toast.show({ message: 'Demande envoyée ! La boutique vous recontactera.', type: 'success' });
      },
      error: () => {
        this.submitting.set(false);
        this.toast.show({ message: 'Échec de l’envoi. Réessayez.', type: 'danger' });
      },
    });
  }
}
