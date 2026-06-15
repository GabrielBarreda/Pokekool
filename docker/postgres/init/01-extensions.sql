-- Extensions PostgreSQL nécessaires à Pokekool.
-- Exécuté automatiquement au PREMIER démarrage du conteneur Postgres
-- (les .sql sous /docker-entrypoint-initdb.d/ sont joués en ordre alphabétique).

-- gen_random_uuid() pour les clés primaires UUID
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Index GIN trigram — recherche fulltext sur les noms de cartes
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Citext pour les emails case-insensitive (on normalise déjà côté domaine, mais utile en BDD)
CREATE EXTENSION IF NOT EXISTS citext;
