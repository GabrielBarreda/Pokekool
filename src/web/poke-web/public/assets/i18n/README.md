# card-names-fr.json

Dictionnaire `TcgId -> nom FR` consommé par `CardDisplayLanguageService`
(voir `src/web/poke-web/src/app/core/services/card-display-language.service.ts`)
pour le réglage Paramètres → Apparence → « Langue d'affichage des cartes ».

**Portée strictement UI/UX** — ce fichier ne remplace jamais `PokemonCard.Name`
en base : il ne fait qu'être consulté côté client pour choisir quel libellé
afficher. Voir ADR-004 (mémoire Obsidian du projet, dossier `07-Decisions`)
pour la justification complète de cette architecture.

## Contenu actuel

Le fichier commité est un **échantillon de démarrage** (une quinzaine de
cartes emblématiques) — suffisant pour développer/tester le réglage, pas pour
la production. À régénérer avant mise en prod via le script
`scripts/generate-card-names-fr.py` (voir sa docstring) qui interroge
l'API TCGdex (`api.tcgdex.net`, gratuite, sans authentification).

## Format

```json
{
  "<TcgId>": "<nom FR>"
}
```

- Clé : `TcgId` — identifiant stable déjà présent sur tous les DTOs actuels
  (`PokemonCardDto.TcgId`, `MarketCardDto.TcgId`, `CollectionCardDto.TcgId`,
  `PokemonCardSummaryDto.TcgId`…). Correspond au même `id` que la ressource
  pokemontcg.io, aligné avec l'`id` TCGdex pour les sets modernes.
- Valeur : nom localisé FR tel que retourné par TCGdex.
- Une carte absente du fichier retombe silencieusement sur le nom anglais
  (`PokemonCard.Name`) côté affichage — comportement normal, pas une erreur.

## Régénération

```bash
python3 scripts/generate-card-names-fr.py --out src/web/poke-web/public/assets/i18n/card-names-fr.json
```

À relancer manuellement après tout import de nouveaux sets côté backend
(pas de synchronisation automatique pour l'instant — voir ADR-004,
section « Conséquences / Compromis »).

## Mobile (MAUI)

Le même dictionnaire doit être copié vers les ressources de l'app mobile
(`src/mobile/Pokekool.Mobile/Resources/`) — voir la note dans ADR-004 sur la
duplication acceptée entre web et mobile. Non automatisé pour l'instant.
