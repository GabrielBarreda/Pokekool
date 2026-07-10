# Prompt UI — Refonte Client Boutique : PLV Carton Pokémon (présentoir physique scrollable)

## Fichier à modifier

`src/app/features/client/pages/boutique/client-boutique.component.ts`

**Ne pas toucher** : logique TypeScript, signaux, `products()`, `productRows()`, `openModal()`, `submit()`, filtres, pagination, modale. Uniquement le **template HTML** du bloc `@if (themeStore.isPokemon())` et les **styles CSS** `.plv-*`.

---

## Référence visuelle cible

Un présentoir de cartes Pokémon physique, vu de face, en portrait :
- **Structure verticale scrollable** — le présentoir est plus haut que l'écran, on scrolle dedans
- **Header** en haut : artwork Pokémon (Rayquaza, ou Pokéball stylisée) sur fond bleu nuit
- **Étagères empilées** : chaque étagère = une rangée de 4 cartes
  - **Fond de niche** avec texture carton cannelé (fentes horizontales) + logo POKÉMON en watermark centré
  - **Sol d'étagère** : gradient de perspective pour donner la profondeur
  - **Cartes au premier plan** : debout sur le sol, même mode visuel que la page Collection (ratio 2.5/3.5, pleine image, coins arrondis)
  - **Étiquette prix** : sous chaque carte, comme une vraie étiquette de slot carton

---

## Nouveau template — remplacer le bloc `@if (themeStore.isPokemon())`

```html
@if (themeStore.isPokemon()) {
  <div class="plv-stand">

    <!-- ── TOP HEADER ───────────────────────────────────────── -->
    <div class="plv-stand__header">
      <div class="plv-stand__header-art" aria-hidden="true">
        <!-- Pokémon artworks en emoji (remplaçables par vraies images) -->
        <span class="plv-art plv-art--1">🐉</span>
        <span class="plv-art plv-art--2">⚡</span>
        <span class="plv-art plv-art--3">🔥</span>
        <span class="plv-art plv-art--4">💎</span>
      </div>
      <div class="plv-stand__header-logo">
        <span class="plv-logo-text">POKÉMON</span>
        <span class="plv-logo-sub">TCG · Boutique officielle</span>
      </div>
      <div class="plv-stand__header-ball" aria-hidden="true"></div>
    </div>

    <!-- ── ÉTAGÈRES (1 par rangée de 4 produits) ────────────── -->
    <div class="plv-stand__body">

      @for (row of productRows(); track $index) {
        <div class="plv-shelf">

          <!-- Fond de niche : texture carton + logo watermark -->
          <div class="plv-shelf__back">
            <span class="plv-shelf__back-logo" aria-hidden="true">POKÉMON</span>
          </div>

          <!-- Sol perspective -->
          <div class="plv-shelf__floor"></div>

          <!-- Liseron avant (bord d'étagère doré) -->
          <div class="plv-shelf__ledge"></div>

          <!-- Cartes debout sur l'étagère -->
          <div class="plv-shelf__cards">
            @for (p of row; track p.id; let i = $index) {
              <button type="button" class="plv-card-holder" (click)="openModal(p)">

                <!-- Image pleine — même mode que Collection (ratio 2.5 / 3.5) -->
                <div class="plv-card-visual">
                  @if (imageOf(p)) {
                    <img class="plv-card-visual__img" [src]="imageOf(p)" [alt]="titleOf(p)" loading="lazy" />
                  } @else {
                    <div class="plv-card-visual__ph">
                      <span class="plv-card-visual__icon">{{ typeIcon(p.type) }}</span>
                    </div>
                  }
                  <!-- Reflet holographique au hover -->
                  <span class="plv-card-visual__sheen" aria-hidden="true"></span>
                </div>

                <!-- Étiquette slot carton -->
                <div class="plv-card-tag">
                  <span class="plv-card-tag__name">{{ titleOf(p) }}</span>
                  <span class="plv-card-tag__grade">
                    @if (p.gradingCompany) { {{ p.gradingCompany }} {{ p.gradeValue ?? '' }}
                    } @else if (p.condition) { {{ conditionLabel(p.condition) }}
                    } @else { {{ typeShort(p.type) }} }
                    @if (p.quantity > 1) { &nbsp;×{{ p.quantity }} }
                  </span>
                  <span class="plv-card-tag__price">{{ formatMoney(p.price) }} {{ p.currency }}</span>
                </div>

              </button>
            }
          </div>

        </div>
      }

    </div>

    <!-- ── BAS DU PRÉSENTOIR ─────────────────────────────────── -->
    <div class="plv-stand__base">
      <span class="plv-stand__base-ball" aria-hidden="true"></span>
      <span class="plv-stand__base-text">Transaction en main propre · Pas de paiement en ligne</span>
      <span class="plv-stand__base-ball" aria-hidden="true"></span>
    </div>

  </div>
}
```

---

## CSS complet — remplacer tous les styles `.plv-*` existants

```css
/* ════════════════════════════════════════════════════════════
   PLV POKÉMON — présentoir carton physique, coté client
   ════════════════════════════════════════════════════════════ */

/* ── Conteneur principal ── */
.plv-stand {
  display: flex;
  flex-direction: column;
  background: #0a0a14;
  border-radius: 16px;
  overflow: hidden;
  border: 3px solid #ffcb05;
  box-shadow:
    0 0 0 6px #0a0a14,
    0 0 0 8px rgba(255,203,5,0.3),
    0 12px 40px rgba(0,0,0,0.6);
  width: 100%;
  max-width: 680px;
  margin: 0 auto;
}

/* ── Header artwork ── */
.plv-stand__header {
  position: relative;
  background: linear-gradient(160deg, #1a2a5e 0%, #0a132e 60%, #060a1c 100%);
  padding: 28px 24px 20px;
  text-align: center;
  overflow: hidden;
  border-bottom: 4px solid #ffcb05;
}
/* Reflet diagonal sur le header */
.plv-stand__header::after {
  content: '';
  position: absolute; inset: 0;
  background: linear-gradient(115deg, transparent 35%, rgba(255,255,255,0.06) 50%, transparent 65%);
  pointer-events: none;
}

.plv-stand__header-art {
  position: absolute;
  top: 50%; left: 50%;
  transform: translate(-50%, -50%);
  display: flex; gap: 4px;
  opacity: 0.15;
  font-size: 64px;
  pointer-events: none;
  filter: blur(1px);
  white-space: nowrap;
}
.plv-art { display: inline-block; }
.plv-art--1 { transform: rotate(-15deg) scale(1.2); }
.plv-art--2 { transform: rotate(5deg); }
.plv-art--3 { transform: rotate(-5deg); }
.plv-art--4 { transform: rotate(12deg) scale(0.9); }

.plv-stand__header-logo {
  position: relative; z-index: 1;
  display: flex; flex-direction: column; align-items: center; gap: 4px;
}
.plv-logo-text {
  font-size: 28px;
  font-weight: 900;
  color: #ffcb05;
  text-shadow:
    2px 2px 0 rgba(0,0,0,0.6),
    0 0 20px rgba(255,203,5,0.4);
  letter-spacing: 4px;
  text-transform: uppercase;
}
.plv-logo-sub {
  font-size: 10px;
  color: rgba(255,255,255,0.5);
  letter-spacing: 2px;
  text-transform: uppercase;
}

/* Pokéball décorative coin bas-droite du header */
.plv-stand__header-ball {
  position: absolute; bottom: 12px; right: 16px;
  width: 32px; height: 32px;
  border-radius: 50%;
  background:
    radial-gradient(circle at 50% 50%, #fff 0 18%, transparent 19%),
    linear-gradient(#ee1515 0 48%, #222 48% 52%, #fff 52% 100%);
  box-shadow: 0 2px 6px rgba(0,0,0,0.5), inset 0 0 0 1px rgba(0,0,0,0.4);
  opacity: 0.6;
}

/* ── Corps scrollable ── */
.plv-stand__body {
  display: flex;
  flex-direction: column;
  gap: 0;
  overflow-y: auto;
  max-height: 70vh;      /* scroll interne, ajuster selon le contexte */
  padding: 0;
  scrollbar-width: thin;
  scrollbar-color: #ffcb05 #060a1c;
}
.plv-stand__body::-webkit-scrollbar { width: 6px; }
.plv-stand__body::-webkit-scrollbar-track { background: #060a1c; }
.plv-stand__body::-webkit-scrollbar-thumb { background: #ffcb05; border-radius: 3px; }

/* ── Étagère individuelle ── */
.plv-shelf {
  position: relative;
  display: flex;
  flex-direction: column;
  /* Séparateur entre étagères */
  border-top: 3px solid #ffcb05;
}
.plv-shelf:first-child { border-top: none; }

/* Fond de niche — texture carton cannelé (fentes horizontales) + logo watermark */
.plv-shelf__back {
  position: relative;
  height: 72px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;

  /* ── Texture carton cannelé (fentes horizontales) ── */
  background:
    /* Ombres des fentes (bords inférieurs) */
    repeating-linear-gradient(
      180deg,
      transparent    0px,
      transparent    7px,
      rgba(0,0,0,0.35) 7px,
      rgba(0,0,0,0.35) 8px,
      rgba(0,0,0,0.15) 8px,
      rgba(0,0,0,0.15) 10px
    ),
    /* Reflets des fentes (bords supérieurs) */
    repeating-linear-gradient(
      180deg,
      rgba(255,255,255,0.06) 0px,
      rgba(255,255,255,0.06) 2px,
      transparent 2px,
      transparent 10px
    ),
    /* Base sombre nuit */
    linear-gradient(160deg, #1a1a3a 0%, #0e0e25 100%);
}

/* Logo POKÉMON en watermark sur fond de niche */
.plv-shelf__back-logo {
  font-size: 36px;
  font-weight: 900;
  color: rgba(255,203,5,0.12);
  letter-spacing: 8px;
  text-transform: uppercase;
  pointer-events: none;
  user-select: none;
  position: relative; z-index: 1;
  /* Contour très subtil */
  text-shadow:
    0 0 1px rgba(255,203,5,0.2),
    1px 0 0 rgba(255,203,5,0.05),
    -1px 0 0 rgba(255,203,5,0.05);
}

/* Sol d'étagère — illusion de perspective */
.plv-shelf__floor {
  height: 28px;
  background: linear-gradient(
    180deg,
    #16163a 0%,         /* continuité avec le fond de niche */
    #1e1e3e 20%,
    #2a2030 60%,
    #3a2a20 85%,        /* teinte bois/carton au sol */
    #2a1e16 100%
  );
  /* Fentes verticales discrètes sur le sol (slots cartes) */
  background-image:
    linear-gradient(
      180deg,
      #16163a 0%, #2a1e16 100%
    ),
    repeating-linear-gradient(
      90deg,
      transparent 0,
      transparent calc(25% - 1px),
      rgba(0,0,0,0.2) calc(25% - 1px),
      rgba(0,0,0,0.2) 25%
    );
  /* On merge les deux backgrounds */
  background-blend-mode: normal;
}

/* Bord avant doré (liseré de façade d'étagère) */
.plv-shelf__ledge {
  height: 6px;
  background: linear-gradient(90deg, #a07a00, #ffcb05 20%, #fff5cc 50%, #ffcb05 80%, #a07a00);
  box-shadow: 0 2px 8px rgba(255,203,5,0.4), 0 1px 0 rgba(0,0,0,0.5);
}

/* ── Grille de cartes sur l'étagère ── */
.plv-shelf__cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0;
  background: #1a1000;
  padding: 12px 12px 16px;
  /* Fentes carton entre chaque slot — séparateurs verticaux */
  background-image:
    repeating-linear-gradient(
      90deg,
      transparent 0,
      transparent calc(25% - 1px),
      rgba(255,203,5,0.12) calc(25% - 1px),
      rgba(255,203,5,0.12) calc(25% + 0px)
    ),
    linear-gradient(#1a1000, #0e0800);
}

/* ── Holder d'une carte ── */
.plv-card-holder {
  all: unset;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0;
  cursor: pointer;
  padding: 0 6px;
  position: relative;
}

/* ── Image carte — même ratio que CollectionCardComponent ── */
.plv-card-visual {
  position: relative;
  aspect-ratio: 2.5 / 3.5;
  width: 100%;
  border-radius: 8px;
  overflow: hidden;
  background: #07111f;
  border: 1.5px solid rgba(255,255,255,0.15);

  /* Profondeur : ombre portée vers le bas = carte en avant-plan */
  box-shadow:
    0 8px 16px rgba(0,0,0,0.6),
    0 3px 6px rgba(0,0,0,0.4),
    inset 0 0 0 1px rgba(255,255,255,0.05);

  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
}

.plv-card-holder:hover .plv-card-visual,
.plv-card-holder:focus-visible .plv-card-visual {
  transform: translateY(-8px) scale(1.03);
  box-shadow:
    0 18px 28px rgba(0,0,0,0.65),
    0 6px 12px rgba(255,203,5,0.2),
    inset 0 0 0 1px rgba(255,203,5,0.4);
  border-color: rgba(255,203,5,0.6);
}

/* Image plein cadre */
.plv-card-visual__img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  transition: transform 0.25s ease;
}
.plv-card-holder:hover .plv-card-visual__img {
  transform: scale(1.05);
}

/* Placeholder sans image */
.plv-card-visual__ph {
  width: 100%; height: 100%;
  display: flex; align-items: center; justify-content: center;
  background: linear-gradient(135deg, #0d1a3a, #07111f);
}
.plv-card-visual__icon {
  font-size: 32px;
  filter: drop-shadow(0 2px 4px rgba(0,0,0,0.5));
  opacity: 0.6;
}

/* Reflet holographique au hover */
.plv-card-visual__sheen {
  position: absolute; inset: 0;
  background: linear-gradient(
    115deg,
    transparent 30%,
    rgba(255,255,255,0) 44%,
    rgba(255,255,255,0.35) 50%,
    rgba(255,255,255,0) 56%,
    transparent 70%
  );
  opacity: 0;
  transition: opacity 0.25s ease;
  pointer-events: none;
}
.plv-card-holder:hover .plv-card-visual__sheen {
  opacity: 1;
}

/* ── Étiquette slot (sous la carte) ── */
.plv-card-tag {
  width: 100%;
  background: #fffdf0;      /* papier crème */
  border: 1px solid rgba(0,0,0,0.15);
  border-top: 2px solid #ffcb05;
  border-radius: 0 0 4px 4px;
  padding: 4px 6px 6px;
  display: flex;
  flex-direction: column;
  gap: 1px;
  /* Légère ombre pour que l'étiquette semble collée sous la carte */
  box-shadow: 0 3px 6px rgba(0,0,0,0.3);
}
.plv-card-tag__name {
  font-size: 9px;
  font-weight: 700;
  color: #1a1a1a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.2;
}
.plv-card-tag__grade {
  font-size: 8px;
  color: #666;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.plv-card-tag__price {
  font-size: 12px;
  font-weight: 900;
  color: #c0392b;
  letter-spacing: -0.02em;
  margin-top: 2px;
}

/* ── Base du présentoir ── */
.plv-stand__base {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 10px 20px;
  background: linear-gradient(90deg, #060a1c, #0a132e, #060a1c);
  border-top: 4px solid #ffcb05;
  font-size: 9px;
  color: rgba(255,255,255,0.35);
  letter-spacing: 1px;
  text-transform: uppercase;
}
.plv-stand__base-ball {
  width: 10px; height: 10px; flex-shrink: 0; border-radius: 50%;
  background:
    radial-gradient(circle at 50% 50%, #fff 0 16%, transparent 17%),
    linear-gradient(#ee1515 0 48%, #222 48% 52%, #fff 52% 100%);
  opacity: 0.4;
}

/* ── Responsive ── */
@media (max-width: 600px) {
  .plv-shelf__cards { grid-template-columns: repeat(2, 1fr); padding: 8px 8px 12px; }
  .plv-logo-text { font-size: 20px; letter-spacing: 2px; }
  .plv-shelf__back-logo { font-size: 22px; letter-spacing: 4px; }
}
@media (max-width: 360px) {
  .plv-shelf__cards { grid-template-columns: repeat(2, 1fr); gap: 0; }
  .plv-card-holder { padding: 0 3px; }
}
```

---

## Ce qui change vs l'existant

| Ancien | Nouveau |
|---|---|
| `plv-tcg-card` en `aspect-ratio: 4/3` (paysage) | `plv-card-visual` en `aspect-ratio: 2.5/3.5` (portrait, comme collection) |
| Header + stats + CTA sur la carte | Image plein cadre uniquement, étiquette séparée sous la carte |
| Fond beige (`#faf6ec`) | Présentoir complet bleu nuit + structure carton |
| Texture inexistante | Fentes carton cannelées en CSS pur (repeating-gradient) |
| Pas de profondeur | Sol perspective + ombres portées + translateY au hover |
| Logo Pokémon absent du fond | Watermark POKÉMON centré dans chaque fond de niche |
| Max 4 colonnes sans containment | Grille dans un stand max-width 680px centré |

## Points d'attention

1. **`productRows()`** — computed existant qui chunk par 4 → **conserver tel quel**, la nouvelle structure l'utilise identiquement.

2. **`openModal(p)`** — le bouton `.plv-card-holder` a `all: unset` pour reset les styles natifs button, mais doit rester `type="button"` et conserver l'event click.

3. **`max-height: 70vh`** sur `.plv-stand__body` — rend le corps scrollable. Ajuster si le layout client a déjà un conteneur scrollable parent (dans ce cas remplacer par `max-height: none` et laisser le scroll de page).

4. **Fond de niche** : le `background-image` avec `repeating-linear-gradient` simule les fentes du carton cannelé sans image externe. Entièrement CSS.

5. **Illustrations Pokémon** — les emojis dans `.plv-stand__header-art` sont floutés/opacifiés comme fond. À remplacer plus tard par de vraies images Pokémon SVG/PNG si disponibles dans les assets.
