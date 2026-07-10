# Anatomie d'une carte Pokémon TCG

Ce fichier décrit la structure visuelle d'une carte standard pour aider le LLM à localiser les informations clés.

## Dimensions et orientation

- Format : 63 mm × 88 mm (aspect ratio ~0.716)
- Orientation : portrait (plus haute que large)
- Coins : arrondis

## Zones d'information — face avant (recto)

```
┌─────────────────────────────────────┐
│  [Nom de la carte]         [PV XXX] │  ← Barre de titre (haut)
│  [Sous-type / règle ex]             │
├─────────────────────────────────────┤
│                                     │
│          [Artwork principal]        │  ← Image centrale (~40% de la carte)
│                                     │
├─────────────────────────────────────┤
│  [Attaques / capacités]             │  ← Corps de texte
│  [Dégâts]                           │
├─────────────────────────────────────┤
│  [Faiblesse] [Résistance] [Retraite]│  ← Pied de carte
│  [Numéro] / [Total]    [Symbole set]│  ← Bas de carte (numérotation + logo set)
│  [Illustrateur] [Copyright]         │
└─────────────────────────────────────┘
```

## Champs à extraire prioritairement

### Nom de la carte
- Position : **barre du haut, à gauche**
- Style : grand, gras, souvent en couleur contrastée
- Exemples : "Pikachu ex", "Charizard VMAX", "Mewtwo GX", "Darkrai ☆"
- Suffixes importants qui font partie du nom : ex, GX, VMAX, V, VSTAR, EX (ancienne ère), ☆, △

### Numéro de carte
- Position : **bas de carte, à gauche ou centre**
- Format : `NNN/TTT` (ex : `025/165`, `199/165` pour les ultra rares > total, `SV001` pour les promos)
- Certaines cartes ont des numéros avec préfixe lettré : `TG01/TG30`, `GG01/GG70`, `SWSH001`

### Symbole de set (extension)
- Position : **bas de carte, à droite du numéro**
- C'est un petit logo/icône unique à chaque extension
- Exemples courants : voir `set-symbols.md`

### PV (Points de Vie)
- Position : **haut à droite**
- Format : "PV 120" ou "HP 120" selon la langue
- Indicateur utile pour distinguer des cartes similaires (même Pokémon, PV différents)

## Éléments visuels distinctifs par rareté

| Rareté | Indicateur visuel |
|--------|-------------------|
| Common ◆ | Fond mat, pas d'effet |
| Uncommon ◆◆ | Fond mat |
| Rare ★ | Fond légèrement brillant |
| Holo Rare ★ | Artwork holographique (reflets arc-en-ciel) |
| Double Rare ★★ | ex, GX : bordure argentée/dorée |
| Ultra Rare ★★★ | Full art, textures dorées/arc-en-ciel |
| Illustration Rare IR | Grand artwork débordant, fond illustré |
| Special Illustration Rare SIR | Artwork pleine carte, textures en relief |
| Hyper Rare HR | Fond doré uni, texte doré |

## Langues

Les cartes existent en plusieurs langues. Indices pour identifier la langue :
- **Japonais** : caractères kanji/hiragana/katakana, format souvent légèrement différent
- **Français** : "PV" pour les points de vie, "Faiblesses", "Résistances", "Coût de retraite"
- **Anglais** : "HP", "Weakness", "Resistance", "Retreat Cost"

La langue impacte le nom de la carte — toujours retourner le nom tel qu'il est imprimé.

## Dos de carte (verso)

- Fond bleu foncé avec Poké Ball stylisée
- Identique pour toutes les cartes depuis 1999 (sauf cartes promo/spéciales)
- Ne contient aucune information utile pour l'identification
