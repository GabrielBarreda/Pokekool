# Pokekool

Plateforme de marché et de gradation de cartes Pokémon — Web + Mobile.

## Stack

| Couche       | Technologie                                   |
|--------------|-----------------------------------------------|
| Backend      | C# / ASP.NET Core Web API (Clean Architecture)|
| Web          | Angular (dernière version stable)             |
| Mobile       | .NET MAUI (iOS + Android)                     |
| Base de données | PostgreSQL 16                              |
| Cache / Sessions | Redis 7                                   |
| ORM          | Entity Framework Core + Npgsql                |
| MVVM Mobile  | CommunityToolkit.Mvvm                         |

## Arborescence

```
pokekool/
├── src/
│   ├── backend/                    ← Solution C# (5 projets Clean Archi)
│   │   ├── Pokekool.Domain
│   │   ├── Pokekool.Application
│   │   ├── Pokekool.Infrastructure
│   │   ├── Pokekool.API
│   │   └── Pokekool.Shared           ← DTOs partagés Backend ↔ MAUI
│   ├── web/pokekool-web/             ← Angular
│   └── mobile/Pokekool.Mobile/       ← .NET MAUI
├── design-system/                  ← tokens.json + scripts d'export
├── docker/                         ← docker-compose dev
└── .github/workflows/              ← CI/CD pipelines
```

Voir [`ARCHITECTURE.md`](./ARCHITECTURE.md) pour le détail complet (architecture, schéma BDD, roadmaps).

## Prérequis

- **.NET SDK** 8.0 ou supérieur
- **Node.js** 20 LTS ou supérieur
- **Docker Desktop** (pour PostgreSQL + Redis en local)
- **Angular CLI** : `npm install -g @angular/cli`
- **EF Core CLI** : `dotnet tool install --global dotnet-ef`
- **.NET MAUI workload** : `dotnet workload install maui`

## Démarrage local

### 1. Infrastructure (PostgreSQL + Redis + Adminer)

```powershell
docker compose -f docker/docker-compose.yml up -d
```

Services exposés :

| Service  | URL / Port                          | Identifiants            |
|----------|-------------------------------------|-------------------------|
| Postgres | `localhost:5434 | pokekool / pokekool_dev_password` |
| Redis    | `localhost:6382`                    | sans mot de passe       |
| Adminer  | http://localhost:8091               | Système : PostgreSQL, Serveur : `postgres / pokekool / BDD : pokekool |

### 2. Backend ASP.NET Core

```powershell
cd src/backend

# Première fois : générer la migration initiale
dotnet ef migrations add Initial `
  --project Pokekool.Infrastructure `
  --startup-project Pokekool.API `
  --output-dir Persistence/Migrations

# Démarrer l'API (les migrations + le seed du tenant 'pokekool-dev' s'exécutent automatiquement en Development)
dotnet run --project Pokekool.API
```

Endpoints disponibles :

| Endpoint                       | Auth         | Rôle                                       |
|--------------------------------|--------------|--------------------------------------------|
| http://localhost:5080/swagger  | —            | Swagger UI (OpenAPI v1)                    |
| `GET /healthz/live`            | —            | Liveness (l'API répond)                    |
| `GET /healthz/ready`           | —            | Readiness (Postgres + Redis joignables)    |
| `GET /healthz`                 | —            | Alias liveness                             |
| `GET /api/v1/ping`             | —            | Test pipeline versionnée                   |
| `POST /api/v1/auth/login`      | —            | Login (email + password + tenantSlug)      |
| `POST /api/v1/auth/refresh`    | —            | Rotation du refresh token                  |
| `POST /api/v1/auth/logout`     | Bearer       | Révocation du refresh token courant        |
| `GET /api/v1/me`               | Bearer       | Contexte de l'utilisateur courant          |
| `GET /api/v1/menus`            | Bearer       | Liste les menus du tenant                  |
| `GET /api/v1/menus/{id}`       | Bearer       | Détail d'un menu (avec catégories + items) |
| `POST /api/v1/menus`           | Bearer (mgr+)| Créer un menu                              |
| `PUT /api/v1/menus/{id}`       | Bearer (mgr+)| Mettre à jour un menu                      |
| `DELETE /api/v1/menus/{id}`    | Bearer (mgr+)| Soft-delete d'un menu                      |
| `GET /api/v1/menus/{id}/categories`  | Bearer | Lister les catégories d'un menu          |
| `POST /api/v1/menus/{id}/categories` | Bearer (mgr+) | Créer une catégorie                |
| `PUT /api/v1/categories/{id}`  | Bearer (mgr+)| Mettre à jour une catégorie                |
| `DELETE /api/v1/categories/{id}` | Bearer (mgr+) | Soft-delete d'une catégorie             |
| `GET /api/v1/categories/{id}/items` | Bearer | Lister les items d'une catégorie          |
| `POST /api/v1/categories/{id}/items` | Bearer (mgr+) | Créer un item                      |
| `PUT /api/v1/items/{id}`       | Bearer (mgr+)| Mettre à jour un item                      |
| `DELETE /api/v1/items/{id}`    | Bearer (mgr+)| Soft-delete d'un item                      |

> `(mgr+)` = rôle `SuperAdmin`, `admin` ou `Manager` requis.

### Identifiants de dev (seedés au premier démarrage)

| Champ         | Valeur                          |
|---------------|---------------------------------|
| TenantSlug    | `pokekool-dev`                    |
| Email         | `admin@pokekool-dev.dev`          |
| Password      | `DevAdmin#2026`                 |
| Rôle          | `admin`               |

### Tester l'auth depuis Swagger UI

1. Ouvre http://localhost:5080/swagger
2. Déclenche `POST /api/v1/auth/login` avec :
   ```json
   {
     "email": "admin@pokekool-dev.dev",
     "password": "DevAdmin#2026",
     "tenantSlug": "pokekool-dev"
   }
   ```
3. Copie l'`accessToken` de la réponse.
4. Clique sur le bouton **Authorize** (cadenas en haut à droite), colle le token (sans préfixe `Bearer`), valide.
5. Déclenche `GET /api/v1/me` → tu dois voir ton `userId`, `tenantId` et le rôle `admin`.

### 3. Design System

Le design system a une **source de vérité unique** : `design-system/tokens.json`. Les scripts d'export
le transforment en CSS variables (Angular) et en ResourceDictionary XAML (MAUI).

```powershell
# Depuis la racine du repo
npm run tokens:build
```

Cela génère deux fichiers (à committer après chaque modification de `tokens.json`) :

| Fichier généré                                                       | Cible   |
|----------------------------------------------------------------------|---------|
| `src/web/pokekool-web/src/styles/tokens.css`                           | Angular |
| `src/mobile/Pokekool.Mobile/Resources/Styles/Tokens.xaml`              | MAUI    |

**Mode watch** (rebuild auto à chaque modification de `tokens.json`) :

```powershell
npm run tokens:watch
```

**Règle stricte** : aucune valeur de couleur, taille, espacement, etc. n'est hardcodée dans
le code Angular ou MAUI — elles passent toutes par les tokens. Voir `ARCHITECTURE.md` §1.5.

**Notes sur le port MAUI** :
- Les **couleurs** sont converties en `#AARRGGBB` (alpha en premier, format XAML).
- Les **durées** deviennent des `TimeSpan` XAML.
- Les **easings cubic-bezier** et les **shadows CSS** ne sont pas portables tel quel — ils sont
  commentés dans `Tokens.xaml` et doivent être recodés manuellement avec `Easing.CubicInOut` /
  `<Shadow Offset="..." Radius="..."/>` selon le besoin.

### 4. Web Angular (à venir)

```powershell
cd src/web/pokekool-web
npm install
npm start
```

### 5. Mobile .NET MAUI (à venir)

```powershell
cd src/mobile/Pokekool.Mobile
dotnet build -t:Run -f net8.0-android
```

## Conventions

- **Branches** : `main` (prod), `develop` (intégration), `feature/<ticket>-<slug>`, `fix/<ticket>-<slug>`
- **Commits** : Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`)
- **Code style** : `.editorconfig` à la racine (LF, UTF-8, 4 espaces .cs, 2 espaces .ts/.js/.json)
- **PR** : review obligatoire + CI verte avant merge

## Architecture

La règle de dépendance Clean Architecture est verrouillée par un test `NetArchTest` : aucune dépendance interdite ne peut être introduite sans casser le build.

Multi-tenant : isolation au niveau PostgreSQL via Row Level Security (RLS) + Global Query Filter EF Core en défense en profondeur.

Détails complets : [`ARCHITECTURE.md`](./ARCHITECTURE.md).

## Licence

Propriétaire. Voir [`LICENSE`](./LICENSE).




