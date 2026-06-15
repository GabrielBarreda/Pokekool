# Pokekool — Document fondateur d'architecture

**Statut** : v1.0 — fondation projet
**Stack** : ASP.NET Core (Clean Archi) + React + PostgreSQL + Redis
**Modèle** : Plateforme de marché et de gradation de cartes Pokémon

---

## Sommaire

1. [Phase 1 — Architecture cible](#phase-1)
2. [Phase 2 — Modèle de données PostgreSQL](#phase-2)
3. [Phase 3 — Roadmaps par axe](#phase-3)
4. [Phase 4 — Plan d'attaque immédiat](#phase-4)

---

<a id="phase-1"></a>
## PHASE 1 — ARCHITECTURE CIBLE

### 1.1 BACKEND — Clean Architecture

#### Règle de dépendance (loi cardinale)

Les dépendances pointent **toujours vers l'intérieur**, jamais vers l'extérieur.

```
┌─────────────────────────────────────────────────────────┐
│  Pokekool.API           (Controllers / Middleware / DI) │
│   └── dépend de Application + Infrastructure + Shared   │
│                                                         │
│  Pokekool.Infrastructure (EF Core / Redis / Identity)   │
│   └── dépend de Application + Domain + Shared           │
│                                                         │
│  Pokekool.Application    (Use Cases / DTOs / Validators)│
│   └── dépend de Domain + Shared UNIQUEMENT              │
│                                                         │
│  Pokekool.Domain         (Entités / VO / Interfaces)    │
│   └── NE DÉPEND DE RIEN (zéro framework, zéro EF)      │
│                                                         │
│  Pokekool.Shared         (DTOs publics / Enums)         │
│   └── NE DÉPEND DE RIEN (pas même de Domain)            │
└─────────────────────────────────────────────────────────┘
```

#### Rôle strict de chaque couche

**Pokekool.Domain** — Le cœur métier
- Entités riches (PokemonCard, CardMarketPrice, CardSale, CardPopulation…)
- Value Objects (Money, GradeValue, CardCondition)
- Interfaces de repositories (ICardRepository, IMarketPriceRepository…)
- Exceptions métier (DomainException, InvalidGradeException…)
- **Interdit** : EF Core, ASP.NET, Newtonsoft, aucune annotation framework

**Pokekool.Application** — Les cas d'usage
- Commands & Queries (pattern CQRS via MediatR)
- Handlers (ImportCardPricesHandler, GetMarketScreenHandler…)
- Validators FluentValidation
- Interfaces des services externes (IEmailService, ICurrentUserService)
- **Interdit** : implémentation concrète d'I/O, EF Core, HTTP

**Pokekool.Infrastructure** — L'implémentation technique
- DbContext EF Core + configurations Fluent API
- Repositories EF Core implémentant les interfaces du Domain
- Migrations EF Core
- Implémentation Redis (cache, refresh tokens, rate limit)
- Clients HTTP (PokémonTCG API, eBay API)
- Identity (ASP.NET Core Identity + JWT issuer)
- **Pourquoi** : on peut remplacer PostgreSQL par autre chose sans toucher Domain/Application

**Pokekool.API** — La surface HTTP
- Controllers minces (≤ 20 lignes par action : valide → envoie au Mediator → retourne)
- Middlewares (ExceptionHandling, Logging Serilog)
- Configuration Swagger / OpenAPI
- Configuration DI (`Program.cs`)
- **Règle** : aucune logique métier dans les controllers

#### Conventions API REST

- **Versioning** : préfixe d'URL `/api/v1/...`
- **Nommage** : ressources au pluriel kebab-case (`/api/v1/cards`, `/api/v1/market-prices`)
- **Verbes HTTP** : GET (lire), POST (créer), PUT (remplacer complet), PATCH (mise à jour partielle), DELETE (supprimer)
- **Pagination** : query params `?page=1&pageSize=20`, plafond `pageSize ≤ 100`
- **Format erreurs** : RFC 7807 Problem Details, uniformisé via middleware

#### Authentification & autorisation

- **Access Token JWT** : durée **15 min**, signé HS256, claims `sub`, `roles`, `exp`. Non stocké côté serveur.
- **Refresh Token** : durée **7 jours**, opaque (random 256 bits base64), stocké dans Redis.
- **Endpoint `/auth/refresh`** : rotation systématique du refresh token.
- **Endpoint `/auth/logout`** : supprime le refresh token de Redis.
- **Stockage côté client** :
  - Web : access token en mémoire (variable JS), refresh token en cookie HttpOnly + Secure + SameSite=Strict

#### Rôles & autorisations

| Rôle          | Périmètre                                               |
|---------------|---------------------------------------------------------|
| `super_admin` | Plateforme entière, gestion utilisateurs globaux        |
| `admin`       | Administration contenu (cartes, prix, gradations)       |
| `user`        | Consultation, watchlist, alertes prix                   |

#### Redis — usages

| Clé / namespace              | Contenu                          | TTL          |
|------------------------------|----------------------------------|--------------|
| `refresh:{hash}`             | Refresh tokens                   | 7 jours      |
| `blacklist:access:{jti}`     | Access tokens révoqués           | ≤ 15 min     |
| `card:price:{cardId}`        | Prix marché mis en cache         | 5 min        |
| `ratelimit:{ip}:{route}`     | Compteurs rate limiting          | 1 min        |
| `market:screen:{setId}`      | Écran marché complet             | 2 min        |

---

### 1.2 SHARED — Pokekool.Shared

**Référencé par** : `Pokekool.API` (dépendance projet).

#### Contenu autorisé

- **DTOs** : `CardDto`, `MarketPriceDto`, `LoginResponse`, `PagedResult<T>`…
- **Enums** : `GradingCompany`, `CardCondition`, `UserRole`…
- **Constantes** : noms de claims, policies, version API.

#### Règles d'or

- **Zéro dépendance NuGet hors `System.*`**
- **Aucune référence** à : EF Core, ASP.NET, FluentValidation, MediatR
- **Pas de logique** : juste des records / classes POCO

---

### 1.3 WEB — React + TypeScript

#### Structure par features

```
pokekool-web/
├── src/
│   ├── app/
│   │   ├── routes.tsx                  ← Routes top-level
│   │   ├── providers.tsx               ← QueryClient, Router, Theme
│   │   ├── core/
│   │   │   ├── auth/
│   │   │   │   ├── AuthContext.tsx
│   │   │   │   ├── useAuth.ts
│   │   │   │   └── ProtectedRoute.tsx
│   │   │   ├── api/
│   │   │   │   └── client.ts           ← axios instance + interceptors
│   │   │   └── query/
│   │   │       └── queryClient.ts
│   │   ├── shared/                     ← Composants réutilisables
│   │   │   ├── components/
│   │   │   ├── hooks/
│   │   │   └── ui/                     ← Design system tokens
│   │   └── features/
│   │       ├── auth/
│   │       ├── market/                 ← Écran bourse (vue TradingView)
│   │       ├── cards/                  ← Catalogue cartes
│   │       ├── card-detail/            ← Fiche carte + historique prix
│   │       ├── grading/                ← Comparateur PSA/CGC/PCA
│   │       └── watchlist/
│   ├── assets/
│   └── styles/
│       ├── tokens.css                  ← Généré depuis tokens.json
│       └── global.css
```

#### Stack

- **Vite** + **React 18** + **TypeScript**
- **TanStack Query** (React Query) — fetching, cache, invalidation
- **React Router v6** — navigation
- **Material UI** — composants + theming
- **Recharts / TradingView Lightweight Charts** — graphiques prix

---

### 1.4 DESIGN SYSTEM

#### tokens.json — source de vérité absolue

Le fichier `design-system/tokens.json` définit toutes les valeurs UI : couleurs, typo, spacing, radius, shadows, motion.

#### Export automatisé

**`export-angular.js`** — génère `src/web/pokekool-web/src/styles/tokens.css`
**`export-maui.js`** — génère `src/mobile/Pokekool.Mobile/Resources/Styles/Tokens.xaml`

Les deux scripts sont lancés via `npm run tokens:build`.

---

<a id="phase-2"></a>
## PHASE 2 — MODÈLE DE DONNÉES PostgreSQL

### Conventions transverses

- **Noms** : `snake_case`, tables au pluriel.
- **Clé primaire** : `id UUID PRIMARY KEY DEFAULT gen_random_uuid()`.
- **Timestamps** : `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`, `updated_at TIMESTAMPTZ`.
- **Index** : sur les colonnes de filtrage fréquent.

### Tables principales

#### `users`
| Colonne          | Type           | Contraintes                          |
|------------------|----------------|--------------------------------------|
| id               | UUID           | PK                                   |
| email            | VARCHAR(255)   | NOT NULL, UNIQUE                     |
| email_normalized | VARCHAR(255)   | NOT NULL, UNIQUE                     |
| password_hash    | VARCHAR(500)   | NOT NULL                             |
| username         | VARCHAR(80)    | NOT NULL, UNIQUE                     |
| is_active        | BOOLEAN        | NOT NULL DEFAULT TRUE                |
| created_at       | TIMESTAMPTZ    | NOT NULL DEFAULT NOW()               |

#### `roles`
| Colonne | Type         | Contraintes      |
|---------|--------------|------------------|
| id      | SMALLINT     | PK               |
| name    | VARCHAR(40)  | NOT NULL, UNIQUE |

#### `permissions`
| Colonne     | Type         | Contraintes      |
|-------------|--------------|------------------|
| id          | SMALLINT     | PK               |
| name        | VARCHAR(80)  | NOT NULL, UNIQUE |
| description | TEXT         |                  |

#### `pokemon_series`
| Colonne      | Type         | Contraintes            |
|--------------|--------------|------------------------|
| id           | UUID         | PK                     |
| tcg_id       | VARCHAR(40)  | NOT NULL, UNIQUE       |
| name         | VARCHAR(120) | NOT NULL               |
| logo_url     | VARCHAR(500) |                        |
| created_at   | TIMESTAMPTZ  | NOT NULL DEFAULT NOW() |

#### `pokemon_sets`
| Colonne        | Type         | Contraintes                      |
|----------------|--------------|----------------------------------|
| id             | UUID         | PK                               |
| series_id      | UUID         | NOT NULL, FK → pokemon_series(id)|
| tcg_id         | VARCHAR(40)  | NOT NULL, UNIQUE                 |
| name           | VARCHAR(120) | NOT NULL                         |
| release_date   | DATE         |                                  |
| total_cards    | INT          |                                  |
| symbol_url     | VARCHAR(500) |                                  |
| logo_url       | VARCHAR(500) |                                  |
| created_at     | TIMESTAMPTZ  | NOT NULL DEFAULT NOW()           |

#### `pokemon_cards`
| Colonne          | Type          | Contraintes                      |
|------------------|---------------|----------------------------------|
| id               | UUID          | PK                               |
| set_id           | UUID          | NOT NULL, FK → pokemon_sets(id)  |
| tcg_id           | VARCHAR(40)   | NOT NULL, UNIQUE                 |
| number           | VARCHAR(20)   | NOT NULL                         |
| name             | VARCHAR(200)  | NOT NULL                         |
| rarity           | VARCHAR(80)   |                                  |
| hp               | INT           |                                  |
| types            | TEXT[]        |                                  |
| subtypes         | TEXT[]        |                                  |
| is_holo          | BOOLEAN       | NOT NULL DEFAULT FALSE           |
| is_reverse_holo  | BOOLEAN       | NOT NULL DEFAULT FALSE           |
| created_at       | TIMESTAMPTZ   | NOT NULL DEFAULT NOW()           |
| updated_at       | TIMESTAMPTZ   |                                  |

Index : `idx_cards_set (set_id)`, `idx_cards_name (name)`.

#### `card_market_prices`
Prix actuels (mis à jour périodiquement).
| Colonne          | Type          | Contraintes                       |
|------------------|---------------|-----------------------------------|
| id               | UUID          | PK                                |
| card_id          | UUID          | NOT NULL, FK → pokemon_cards(id)  |
| source           | VARCHAR(40)   | NOT NULL (pokemontcg, ebay, cardmarket, pricecharting) |
| condition        | VARCHAR(20)   | NOT NULL (raw, psa10, psa9, cgc10, cgc9, pca10) |
| price            | NUMERIC(12,2) | NOT NULL                          |
| currency         | CHAR(3)       | NOT NULL DEFAULT 'EUR'            |
| fetched_at       | TIMESTAMPTZ   | NOT NULL DEFAULT NOW()            |

Index : `idx_prices_card_source (card_id, source, condition)`.

#### `card_historical_prices`
Historique pour les graphiques.
| Colonne    | Type          | Contraintes                       |
|------------|---------------|-----------------------------------|
| id         | BIGSERIAL     | PK                                |
| card_id    | UUID          | NOT NULL, FK → pokemon_cards(id)  |
| source     | VARCHAR(40)   | NOT NULL                          |
| condition  | VARCHAR(20)   | NOT NULL                          |
| price      | NUMERIC(12,2) | NOT NULL                          |
| currency   | CHAR(3)       | NOT NULL                          |
| recorded_at| TIMESTAMPTZ   | NOT NULL                          |

Index : `idx_hist_card_date (card_id, condition, recorded_at DESC)`.

#### `card_populations`
Population reports PSA/CGC/PCA.
| Colonne          | Type          | Contraintes                       |
|------------------|---------------|-----------------------------------|
| id               | UUID          | PK                                |
| card_id          | UUID          | NOT NULL, FK → pokemon_cards(id)  |
| grading_company  | VARCHAR(20)   | NOT NULL (psa, cgc, pca)          |
| grade            | VARCHAR(10)   | NOT NULL (10, 9, 8, ...)          |
| count            | INT           | NOT NULL                          |
| fetched_at       | TIMESTAMPTZ   | NOT NULL DEFAULT NOW()            |

#### `audit_logs`
| Colonne     | Type         | Contraintes                              |
|-------------|--------------|------------------------------------------|
| id          | BIGSERIAL    | PK                                       |
| user_id     | UUID         |                                          |
| entity_type | VARCHAR(80)  | NOT NULL                                 |
| entity_id   | UUID         | NOT NULL                                 |
| action      | VARCHAR(40)  | NOT NULL (create, update, delete)        |
| changes     | JSONB        | NOT NULL                                 |
| ip_address  | INET         |                                          |
| created_at  | TIMESTAMPTZ  | NOT NULL DEFAULT NOW()                   |

---

<a id="phase-3"></a>
## PHASE 3 — ROADMAPS PAR AXE

**Légende complexité** : S = 1-2h | M = ½ journée | L = 1-2 jours | XL = 1 semaine+

### ROADMAP 1 — BACKEND (ASP.NET Core + EF Core + PostgreSQL + Redis)

```
[1.1]  Solution + 5 projets Clean Archi                             — S
[1.2]  docker-compose : Postgres + Redis + adminer                  — S
[1.3]  Setup EF Core + DbContext + 1ʳᵉ migration (users/roles)     — M
[1.4]  Pokekool.Shared : DTOs Auth + Enums communs                  — S
[1.5]  Auth : Register/Login + JWT issuer                           — M
[1.6]  Refresh tokens dans Redis + /auth/refresh                    — M
[1.7]  Import PokémonTCG API : séries, sets, cartes                 — L
[1.8]  Import prix eBay (sold listings) + regex grade               — L
[1.9]  Module Market Screen : endpoint agrégé par set               — L
[1.10] Historique prix + endpoint graphique                         — M
[1.11] Population reports PSA/CGC/PCA                               — M
[1.12] Cache Redis sur market screen + invalidation                 — M
[1.13] Rate limiting + Swagger propre                               — M
[1.14] Tests d'intégration (Testcontainers PostgreSQL)              — XL
```

### ROADMAP 2 — WEB APP (React + TypeScript)

```
[2.1] Init Vite + React + TypeScript + TanStack Query               — S
[2.2] Setup auth : login/logout + token en mémoire                 — M
[2.3] Interceptor axios : injection token + refresh sur 401        — M
[2.4] Layout principal (sidebar + topbar)                           — M
[2.5] Feature Market Screen (écran bourse style TradingView)       — XL
[2.6] Feature Catalogue cartes (filtres, recherche, pagination)    — L
[2.7] Feature Fiche carte (prix, graphique, populations)           — L
[2.8] Feature Comparateur gradation (PSA/CGC/PCA)                  — M
[2.9] Watchlist & alertes prix                                      — L
[2.10] Internationalisation FR / EN                                 — M
```

### ROADMAP 3 — DESIGN SYSTEM

```
[3.1] tokens.json v1 (couleurs Pokekool, typo, spacing)            — S
[3.2] Script export tokens.css                                      — S
[3.3] Intégration Material UI + tokens                              — M
[3.4] Composants : Button, Input, Card, Modal, Toast                — M
[3.5] Composants : Table, Badge, Tabs, Skeleton                     — M
```

### ROADMAP 4 — INFRASTRUCTURE

```
[4.1] docker-compose.yml dev (Postgres + Redis + adminer)           — S
[4.2] Dockerfile multi-stage pour Pokekool.API                      — M
[4.3] GitHub Actions : CI backend (build + tests)                   — M
[4.4] GitHub Actions : CI web (lint + build)                        — M
[4.5] Environments : dev / staging / prod                           — M
[4.6] Déploiement backend (Fly.io ou Render)                        — L
[4.7] Déploiement web (Cloudflare Pages / Vercel)                   — M
[4.8] Migrations EF Core jouées au déploiement                      — M
[4.9] Monitoring : OpenTelemetry → Grafana                          — L
```

---

<a id="phase-4"></a>
## PHASE 4 — PLAN D'ATTAQUE IMMÉDIAT

### TÂCHE 1 — Fondations auth (fait ✓)

Solution `Pokekool.sln` avec 5 projets Clean Architecture. Auth JWT + Refresh Token Redis opérationnelle. Docker Compose avec Postgres (5434) + Redis (6382) + Adminer (8091).

**Identifiants dev** : `admin@pokekool.dev` / `DevAdmin#2026`

---

### TÂCHE 2 — Import données PokémonTCG API

**Quoi faire** :
- Client HTTP vers `https://api.pokemontcg.io/v2/`
- Import des séries → sets → cartes dans PostgreSQL
- Command `SyncPokemonTcgCommand` planifiée (background job ou endpoint admin)
- Mapping vers entités Domain (`PokemonSeries`, `PokemonSet`, `PokemonCard`)

**Livrable** : `POST /api/v1/admin/sync/pokemontcg` seed la BDD avec les données TCG complètes.

---

### TÂCHE 3 — Agrégation prix (eBay Sold Listings)

**Quoi faire** :
- Client eBay Browse API ou Finding API (sold listings)
- Regex d'extraction du grade depuis le titre (`PSA 10`, `CGC 9.5`, etc.)
- Stockage dans `card_market_prices` + `card_historical_prices`
- Job de mise à jour toutes les 6h

**Livrable** : les prix de vente récents d'une carte sont visibles via `GET /api/v1/cards/{id}/prices`.

---

### TÂCHE 4 — Market Screen endpoint

**Quoi faire** :
- Endpoint `GET /api/v1/market/sets/{setId}` retourne toutes les cartes d'un set avec leurs prix actuels
- Vue matérialisée PostgreSQL pour la performance
- Cache Redis 2 min

**Livrable** : le front peut afficher un écran "bourse" complet pour un set donné.

---

### TÂCHE 5 — Front React Market Screen

**Quoi faire** :
- Init Vite + React + TypeScript
- Auth (login + refresh)
- Page Market Screen consommant l'endpoint du backend
- Tableau trié par variation de prix (comme TradingView)

**Livrable** : `npm run dev` → écran bourse fonctionnel avec données réelles.

---

## Pourquoi cet ordre

Tâche 1 (auth) → fondations solides, personne ne peut accéder sans token. Tâche 2 (TCG data) → sans données cartes, rien d'autre ne peut fonctionner. Tâche 3 (prix) → la valeur principale de la plateforme. Tâche 4 (market endpoint) → expose les données de façon optimisée. Tâche 5 (front) → le produit devient visible.
