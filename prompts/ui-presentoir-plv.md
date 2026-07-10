# Prompt UI — Refonte Présentoir : PLV Carton Pokémon (mode grille cards)

## Fichier à modifier

`src/app/features/boutique/pages/presentoir/presentoir.component.ts`

**Ne pas toucher** : logique TypeScript, signaux, modales (création/édition/suppression), filtres, `ProductsStore`, `BoutiqueService`. Uniquement le **template HTML** de la liste (section `@else` du bloc `@if store.loading()`) et les **styles CSS** associés.

---

## Référence visuelle — ce que "mode collection" veut dire

Dans `CollectionCardComponent`, chaque carte est affichée comme un vrai rectangle (ratio 2.5/3.5), image plein cadre, hover avec `translateY(-2px)` + shadow. C'est exactement ce mode qu'on veut pour le présentoir — pas un tableau, mais une grille de cartes visuelles.

---

## Ambiance générale — PLV carton Hay Day / Pokémon

**Références visuelles à combiner :**
- L'échoppe Hay Day : présentoir en bois brun, planches horizontales, articles posés dessus, étiquette de prix accrochée
- Le design PLV carton Pokémon officiel : fond bleu nuit très sombre (`#060a1c`), bandes dorées (`#ffcb05`), coins arrondis généreux, Pokéball décorative, texture carton simulée
- Les vitrines de boutiques Pokémon japonaises : chaque carte posée sur un support incliné, étiquette prix en bas

**Le résultat doit donner l'impression** d'un vrai présentoir physique de boutique, scanné en vertical sur écran — pas d'un tableau de données.

---

## Structure HTML cible

Remplacer le bloc `<div class="tbl-wrap"> ... </div>` par cette structure :

```
<div class="plv-board">                              ← panneau carton principal

  <div class="plv-shelf">                            ← étagère (1 seule en V1)

    <div class="plv-shelf-label">                    ← liseur/étiquette d'étagère
      À vendre · {{ store.items().length }} articles
    </div>

    <div class="plv-grid">                           ← grille de cartes

      @for (p of store.items(); track p.id) {
        <div class="plv-card-slot"
             [class.plv-card-slot--unavailable]="!p.isAvailable">

          <!-- Image plein cadre (style CollectionCardComponent) -->
          <div class="plv-card-img-wrap">
            @if (p.photoUrl || p.cardImage) {
              <img class="plv-card-img" [src]="p.photoUrl || p.cardImage" [alt]="p.cardName || p.productName" loading="lazy" />
            } @else {
              <div class="plv-card-placeholder">🃏</div>
            }

            <!-- Badge dispo (coin supérieur droit) -->
            <span class="plv-dispo-badge" [class.plv-dispo-badge--off]="!p.isAvailable">
              {{ p.isAvailable ? '●' : '○' }}
            </span>

            <!-- Overlay grisé si indisponible -->
            @if (!p.isAvailable) {
              <div class="plv-unavail-overlay">Épuisé</div>
            }
          </div>

          <!-- Étiquette prix (style price tag carton) -->
          <div class="plv-price-tag">
            <div class="plv-price-tag__name">{{ p.cardName || p.productName }}</div>
            <div class="plv-price-tag__sub">
              {{ p.gradingCompany ? p.gradingCompany + ' ' + (p.gradeValue ?? '') : (conditionLabel(p.condition) || typeLabels[p.type]) }}
              @if (p.quantity > 1) { · ×{{ p.quantity }} }
            </div>
            <div class="plv-price-tag__price">{{ p.price | number:'1.2-2' }} {{ p.currency }}</div>
          </div>

          <!-- Actions au hover -->
          <div class="plv-card-actions">
            <label class="plv-toggle" [title]="p.isAvailable ? 'Désactiver' : 'Activer'">
              <input type="checkbox" [checked]="p.isAvailable" [disabled]="p.quantity === 0" (change)="toggle(p)" />
              <span>{{ p.isAvailable ? 'En vente' : 'Inactif' }}</span>
            </label>
            <button *hasPermission="'shop:write'" class="plv-ic" (click)="openEdit(p)" title="Modifier">✎</button>
            <button *hasPermission="'shop:write'" class="plv-ic plv-ic--danger" (click)="deleteTarget.set(p)" title="Supprimer">🗑</button>
          </div>

        </div>
      }

    </div>
  </div>
</div>
```

---

## CSS cible — styles à créer/remplacer

### Panneau PLV principal

```css
.plv-board {
  background:
    repeating-linear-gradient(
      0deg,
      transparent,
      transparent 3px,
      rgba(255,203,5,0.03) 3px,
      rgba(255,203,5,0.03) 4px
    ),
    linear-gradient(160deg, #16234f 0%, #0a132e 60%, #060a1c 100%);
  border-radius: 16px;
  padding: 24px 20px;
  box-shadow:
    0 8px 32px rgba(0,0,0,0.5),
    inset 0 1px 0 rgba(255,203,5,0.15),
    inset 0 -1px 0 rgba(0,0,0,0.3);
  border: 2px solid rgba(255,203,5,0.25);
}
```

### Étagère

```css
.plv-shelf {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.plv-shelf-label {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: linear-gradient(90deg, #ffcb05, #f0a500);
  color: #060a1c;
  font-weight: 700;
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  padding: 4px 14px;
  border-radius: 4px;
  align-self: flex-start;
  box-shadow: 0 2px 6px rgba(0,0,0,0.4);
}
```

### Grille de cartes

```css
.plv-grid {
  display: grid;
  /* 6 colonnes à 160px, responsive vers 4 puis 3 sur petits écrans */
  grid-template-columns: repeat(auto-fill, minmax(148px, 1fr));
  gap: 20px;
}
```

### Slot carte (conteneur)

```css
.plv-card-slot {
  display: flex;
  flex-direction: column;
  gap: 0;
  border-radius: 10px;
  overflow: hidden;
  background: #0d1a3a;
  border: 2px solid rgba(255,203,5,0.3);
  box-shadow: 0 4px 16px rgba(0,0,0,0.4);
  transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
  cursor: default;
  position: relative;
}

.plv-card-slot:hover {
  transform: translateY(-4px) scale(1.01);
  box-shadow: 0 8px 28px rgba(0,0,0,0.55), 0 0 0 1px rgba(255,203,5,0.6);
  border-color: rgba(255,203,5,0.7);
}

.plv-card-slot--unavailable {
  opacity: 0.55;
  filter: saturate(0.4);
}
```

### Image de la carte

```css
.plv-card-img-wrap {
  position: relative;
  aspect-ratio: 2.5 / 3.5;   /* même ratio que CollectionCardComponent */
  overflow: hidden;
  background: #07111f;
}

.plv-card-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  transition: transform 0.2s ease;
}

.plv-card-slot:hover .plv-card-img {
  transform: scale(1.04);
}

.plv-card-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 36px;
  background: linear-gradient(135deg, #0d1a3a, #07111f);
  color: rgba(255,203,5,0.25);
}

/* Badge dispo — point coloré coin sup droit */
.plv-dispo-badge {
  position: absolute;
  top: 6px;
  right: 6px;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #22c55e;
  box-shadow: 0 0 6px rgba(34,197,94,0.7);
  font-size: 0;    /* cache le texte, on utilise le fond coloré */
}
.plv-dispo-badge--off {
  background: #ef4444;
  box-shadow: 0 0 6px rgba(239,68,68,0.6);
}

/* Overlay "Épuisé" */
.plv-unavail-overlay {
  position: absolute;
  inset: 0;
  background: rgba(0,0,0,0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ef4444;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  backdrop-filter: grayscale(0.8);
}
```

### Étiquette prix (sous l'image)

Style "price tag carton" — coin supérieur gauche troué comme une vraie étiquette de boutique.

```css
.plv-price-tag {
  background: #fffdf0;              /* carton crème */
  color: #1a1a1a;
  padding: 8px 10px 10px;
  position: relative;
  border-top: 3px solid #ffcb05;
}

/* Encoche de "fil d'accroche" simulée */
.plv-price-tag::before {
  content: '';
  position: absolute;
  top: -3px;
  left: 50%;
  transform: translateX(-50%);
  width: 20px;
  height: 3px;
  background: #ffcb05;
}

.plv-price-tag__name {
  font-size: 11px;
  font-weight: 700;
  color: #1a1a1a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.3;
}

.plv-price-tag__sub {
  font-size: 9px;
  color: #666;
  margin-top: 2px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.plv-price-tag__price {
  font-size: 15px;
  font-weight: 800;
  color: #c0392b;     /* rouge vif = prix de vente */
  margin-top: 4px;
  letter-spacing: -0.02em;
}
```

### Actions au hover (apparaissent sur survol du slot)

```css
.plv-card-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 8px;
  background: rgba(6,10,28,0.95);
  border-top: 1px solid rgba(255,203,5,0.15);
  opacity: 0;
  transition: opacity 0.15s ease;
}

.plv-card-slot:hover .plv-card-actions {
  opacity: 1;
}

.plv-ic {
  border: none;
  background: rgba(255,255,255,0.08);
  color: rgba(255,255,255,0.75);
  cursor: pointer;
  font-size: 14px;
  padding: 3px 7px;
  border-radius: 4px;
  transition: background 0.15s, color 0.15s;
}
.plv-ic:hover { background: rgba(255,255,255,0.15); color: #fff; }
.plv-ic--danger:hover { background: rgba(239,68,68,0.2); color: #ef4444; }

.plv-toggle {
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  flex: 1;
}
.plv-toggle input { display: none; }
.plv-toggle span {
  font-size: 9px;
  font-weight: 700;
  text-transform: uppercase;
  color: rgba(255,255,255,0.5);
  letter-spacing: 0.05em;
  padding: 2px 6px;
  border-radius: 3px;
  border: 1px solid rgba(255,255,255,0.15);
  transition: all 0.15s;
}
.plv-toggle input:checked + span {
  color: #22c55e;
  border-color: rgba(34,197,94,0.4);
  background: rgba(34,197,94,0.08);
}
```

---

## Points d'attention

1. **La table existante est entièrement supprimée** dans la section liste — remplacée par `plv-board > plv-shelf > plv-grid`. Aucune colonne, aucun `<th>`.

2. **Les modales et formulaires ne changent pas** — seule la liste change. La modale création/édition garde exactement le même HTML/CSS.

3. **Les filtres restent au-dessus** tels quels (`<div class="filters">...`).

4. **`themeStore.isPokemon()`** — ce nouveau style PLV **remplace** la branche `@else` (pas de condition thème). Il s'applique toujours, quel que soit le thème. Le `plv-banner` existant peut être conservé ou supprimé au choix.

5. **Pagination** — si `ProductsStore` gère la pagination, ajouter le bloc pagination (boutons précédent/suivant) **sous** `plv-board`, en dehors du carton.

6. **Empty state** — `<app-empty-state>` reste tel quel dans son bloc `@else if`.
