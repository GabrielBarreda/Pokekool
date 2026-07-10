# scan-knowledge — Base RAG pour la détection de cartes

Ce dossier est la base de connaissance du système de scan. Il est lu par le backend avant chaque appel au LLM Vision (Ollama) pour enrichir le prompt avec du contexte métier (RAG), et mis à jour automatiquement après chaque scan confirmé par l'utilisateur.

## Structure

```
scan-knowledge/
  README.md               ← ce fichier
  stats.md                ← métriques globales (mis à jour par le backend)
  hints/
    grading-slabs.md      ← comment lire les étiquettes PSA / BGS / CGC
    set-symbols.md        ← identifier visuellement les extensions Pokémon
    card-anatomy.md       ← anatomie d'une carte (où lire nom, numéro, set)
  cards/
    <cardId>-<slug>.md    ← une fiche par carte déjà scannée et confirmée
```

## Convention des fiches `cards/`

Chaque fichier est nommé `<cardId>-<name-slug>.md` où `cardId` est le Guid interne de la carte dans la base de données, et `name-slug` est le nom de la carte en kebab-case.

### Frontmatter YAML (lu par le backend)

```yaml
---
cardId: 3f7a1b2c-...
name: Pikachu ex
set: Scarlet & Violet 151
number: 025/165
scanCount: 5
avgConfidence: 0.91
lastSeen: 2026-07-05
---
```

### Sections Markdown (injectées dans le prompt LLM)

- `## Indices visuels confirmés` : descriptions textuelles des éléments visuels distinctifs, enrichies à chaque scan
- `## Grading observé` : notes sur les slabs observés pour cette carte spécifique
- `## Confusions passées` : cartes avec lesquelles le LLM a confondu cette carte, et comment les distinguer

## Convention `stats.md`

```yaml
---
totalScans: 0
totalConfirmed: 0
lastScan: null
---
```

## Comportement du backend

**Avant le scan :**
1. Lit les 3 fichiers `hints/` et les concatène
2. Liste les fiches `cards/` triées par `lastSeen` desc — prend les 15 plus récentes
3. Extrait `name`, `set`, `number`, `avgConfidence` + section `## Indices visuels confirmés` de chaque fiche
4. Construit un bloc texte injecté dans le prompt Ollama Vision

**Après confirmation utilisateur :**
1. Pour chaque carte ajoutée : crée ou met à jour `cards/<id>.md`
2. Met à jour `stats.md`

## Enrichissement manuel

Les sections `## Indices visuels confirmés` peuvent être enrichies manuellement dans Obsidian pour améliorer la précision du LLM. Plus la description est précise (couleurs, textures holographiques, position du symbole de set, typo spécifique), meilleure sera la détection.
