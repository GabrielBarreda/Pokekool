#!/usr/bin/env python3
"""
Génère assets/i18n/card-names-fr.json : dictionnaire TcgId -> nom FR.

Portée strictement UI/UX (voir ADR-004, mémoire Obsidian du projet,
dossier 07-Decisions) : ce script ne touche à AUCUNE donnée backend/DB.
Il produit uniquement un fichier statique consommé par le front pour
l'affichage du réglage "Langue d'affichage des cartes".

Source : TCGdex (https://api.tcgdex.net) — API REST publique, gratuite,
sans authentification, déjà recommandée pour le référentiel FR dans
Multilingue-Cartes.md.

Stratégie :
  1. Récupérer la liste des sets (GET /v2/fr/sets) pour connaître les IDs
     de set côté TCGdex.
  2. Pour chaque set, GET /v2/fr/sets/{setId} renvoie toutes les cartes
     du set en un seul appel (nom localisé FR inclus), plutôt que
     d'interroger carte par carte (~20-25k cartes au total, cf.
     API-PokemonTCG.md "Volume estimé" — interroger set par set réduit le
     nombre de requêtes à ~150 environ).
  3. Le TcgId (identifiant pokemontcg.io, clé utilisée par le front) est
     identique à l'id TCGdex pour les sets modernes. Pour les sets très
     anciens (ère WOTC), un écart de numérotation FR/EN est possible :
     toute carte dont le mapping échoue est simplement omise du fichier
     de sortie (repli EN géré côté client, voir CardDisplayLanguageService).

Usage :
    python3 generate-card-names-fr.py --out path/to/card-names-fr.json
    python3 generate-card-names-fr.py --out ... --set-ids base1,base2   # sous-ensemble, pour test

Nécessite : requests (pip install requests)
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

try:
    import requests
except ImportError:
    print("Ce script nécessite 'requests' : pip install requests", file=sys.stderr)
    sys.exit(1)

TCGDEX_BASE = "https://api.tcgdex.net/v2/fr"
REQUEST_TIMEOUT = 15
RETRY_COUNT = 3
RETRY_DELAY_S = 1.5


def fetch_json(url: str) -> dict | list:
    last_exc: Exception | None = None
    for attempt in range(1, RETRY_COUNT + 1):
        try:
            resp = requests.get(url, timeout=REQUEST_TIMEOUT)
            resp.raise_for_status()
            return resp.json()
        except Exception as exc:  # noqa: BLE001 - on log et on retry
            last_exc = exc
            if attempt < RETRY_COUNT:
                time.sleep(RETRY_DELAY_S * attempt)
    raise RuntimeError(f"Échec après {RETRY_COUNT} tentatives sur {url}") from last_exc


def list_set_ids() -> list[str]:
    sets = fetch_json(f"{TCGDEX_BASE}/sets")
    assert isinstance(sets, list)
    return [s["id"] for s in sets if "id" in s]


def fetch_set_card_names(set_id: str) -> dict[str, str]:
    """Retourne {tcgId: nom_fr} pour toutes les cartes d'un set."""
    data = fetch_json(f"{TCGDEX_BASE}/sets/{set_id}")
    cards = data.get("cards", []) if isinstance(data, dict) else []
    result: dict[str, str] = {}
    for c in cards:
        card_id = c.get("id")
        name = c.get("name")
        if card_id and name:
            result[card_id] = name
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", required=True, help="Chemin de sortie du fichier JSON")
    parser.add_argument("--set-ids", help="Liste de set IDs séparés par des virgules (sinon: tous les sets)")
    args = parser.parse_args()

    set_ids = args.set_ids.split(",") if args.set_ids else list_set_ids()
    print(f"{len(set_ids)} sets à traiter…", file=sys.stderr)

    names_fr: dict[str, str] = {}
    failed_sets: list[str] = []
    for i, set_id in enumerate(set_ids, 1):
        try:
            names_fr.update(fetch_set_card_names(set_id))
            print(f"[{i}/{len(set_ids)}] {set_id} OK ({len(names_fr)} noms cumulés)", file=sys.stderr)
        except Exception as exc:  # noqa: BLE001
            failed_sets.append(set_id)
            print(f"[{i}/{len(set_ids)}] {set_id} ÉCHEC: {exc}", file=sys.stderr)

    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", encoding="utf-8") as f:
        json.dump(names_fr, f, ensure_ascii=False, indent=2, sort_keys=True)

    print(f"\n{len(names_fr)} noms FR écrits dans {out_path}", file=sys.stderr)
    if failed_sets:
        print(f"Sets en échec (repli EN pour leurs cartes) : {', '.join(failed_sets)}", file=sys.stderr)


if __name__ == "__main__":
    main()
