# Lecture des slabs de gradation

Un "slab" est un boîtier plastique rigide scellé dans lequel une société de gradation enferme une carte après évaluation. Ce fichier décrit comment identifier visuellement chaque société et lire les informations imprimées sur l'étiquette.

---

## PSA — Professional Sports Authenticator

### Identification visuelle
- **Boîtier** : plastique transparent rigide, forme rectangulaire standard
- **Étiquette** : rectangle coloré en haut du boîtier (occupe environ 20% du slab)
- **Couleur de l'étiquette selon la note** :
  - **Rouge/bordeaux** : notes 1 à 6 (PSA 1 Poor → PSA 6 EX-MT)
  - **Bleu foncé** : note 7 (PSA 7 NM)
  - **Bleu marine** : note 8 (PSA 8 NM-MT)
  - **Or/doré** : note 9 (PSA 9 MINT)
  - **Rouge vif avec bordure dorée** : note 10 (PSA 10 GEM MINT) — la plus reconnaissable
- **Logo** : "PSA" en lettres blanches grandes à gauche de l'étiquette

### Informations à lire
```
PSA  [NOM DE LA CARTE]
     [NOM DU SET / ANNÉE]
MINT 10              [NOM DU JEU]
CERTIFICATION No. 12345678
```

- **Note** : texte en grand au centre-gauche ("MINT 10", "GEM MINT 10", "NM-MT 8", etc.)
- **Numéro de certification** : suite de 8 chiffres après "CERTIFICATION No." ou "CERT #"
- **Nom de la carte** : imprimé sur l'étiquette au-dessus de la note

### Correspondances texte → valeur numérique
| Texte PSA | Valeur |
|-----------|--------|
| POOR 1 | 1 |
| GOOD 2 | 2 |
| VG 3 | 3 |
| VG-EX 4 | 4 |
| EX 5 | 5 |
| EX-MT 6 | 6 |
| NM 7 | 7 |
| NM-MT 8 | 8 |
| MINT 9 | 9 |
| GEM MINT 10 | 10 |

---

## BGS — Beckett Grading Services

### Identification visuelle
- **Boîtier** : plastique épais, légèrement plus grand que PSA, avec vis aux coins
- **Étiquette** : grande étiquette occupant le tiers supérieur du slab
- **Couleur selon la note** :
  - **Noir** : notes standard (BGS 1 à 9)
  - **Or/doré brillant** : BGS 9.5 (GEM MINT) — très recherché
  - **Noir avec bordure noire épaisse** : BGS 10 (PRISTINE) — extrêmement rare
  - **Blanc** : BGS Black Label 10 (4 sous-notes à 10) — le graal
- **Logo** : "BGS" ou "BECKETT" en haut de l'étiquette

### Spécificité BGS : les sous-notes
BGS attribue 4 sous-notes en plus de la note globale :
```
BGS  [NOTE GLOBALE]
     Centering: 9.5
     Corners:   9.0
     Edges:     9.5
     Surface:   9.5
CERTIFICATION No. 0012345678
```
La note globale est la **plus petite valeur affichée en grand**.

### Informations à lire
- **Note globale** : grand chiffre central (peut être décimal : 8.5, 9, 9.5)
- **Numéro de certification** : 10 chiffres, souvent sous le terme "CERTIFICATION" ou "CERT"

---

## CGC — Certified Guaranty Company

### Identification visuelle
- **Boîtier** : plastique transparent, format similaire à PSA
- **Étiquette** : bande horizontale colorée
- **Couleur de l'étiquette selon la note** :
  - **Violet/mauve** : notes 1 à 7
  - **Bleu** : note 8 (Near Mint/Mint)
  - **Vert clair** : note 9 (MINT)
  - **Or/vert doré** : note 9.5 (GEM MINT)
  - **Jaune vif** : note 10 (PRISTINE)
- **Logo** : "CGC" dans un rectangle à gauche de l'étiquette

### Informations à lire
```
CGC  [NOTE]  [LABEL TYPE]
     [NOM DE LA CARTE]
     [SET]
CERT: 1234567890
```

- **Note** : chiffre en grand (entier ou décimal)
- **Label type** : "Pristine", "Gem Mint", "Near Mint/Mint", etc.
- **Numéro de certification** : après "CERT:"

---

## ACE (Authentication Certification Experts)

- **Boîtier** : similaire à PSA
- **Étiquette** : blanche avec logo ACE rouge
- **Notes** : 1 à 10 (entiers uniquement)
- Moins courant, principalement présent sur le marché européen

---

## Règles de lecture pour le LLM

1. **Identifier d'abord la société** (PSA / BGS / CGC / ACE) via la couleur de l'étiquette et le logo
2. **Lire la note** : c'est le grand chiffre ou texte sur l'étiquette
3. **Lire le numéro de certification** : suite de chiffres après "CERT" ou "CERTIFICATION No."
4. **Lire le nom de la carte** sur l'étiquette (peut différer légèrement du texte sur la carte elle-même)
5. Si le slab est opaque, mal éclairé, ou si l'étiquette est partiellement cachée → `gradingCompany: null`

## Signes qu'une carte est dans un slab

- Reflets caractéristiques du plastique épais
- Bords du boîtier visibles autour de la carte
- Étiquette colorée en haut ou en bas
- La carte paraît "suspendue" dans le boîtier sans contact avec les bords
