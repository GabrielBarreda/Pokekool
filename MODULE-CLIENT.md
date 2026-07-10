# Module Client — espace authentifié (`/client/...`)

Implémentation du prompt `prompts/agent-module-client.md`. Développé en parallèle du
Module Boutique (`prompts/agent-module-boutique.md`) — ce document décrit le périmètre
livré, le **point d'intégration** avec le module Boutique, et les actions dev restantes.

## Périmètre livré

### Backend
- **Entité** `PurchaseRequest` (`Domain/Entities/Shop/PurchaseRequest.cs`) + enum `PurchaseRequestStatus`,
  factory `Create(...)`, dénormalisation `ClientName`/`ClientEmail`, `ChangeStatus(...)`.
- **EF config** `Infrastructure/.../Configurations/Shop/PurchaseRequestConfiguration.cs` → table `purchase_request`
  (FK `shop_product_id`, FK `user_id`, statut stocké en string).
- **DbSet** `PurchaseRequests` ajouté à `PokekoolDbContext`.
- **Repositories** : `IPurchaseRequestRepository` + impl ; `IShopClientRepository` + impl (le **seam** Boutique).
- **CQRS** (`Application/Shop/...` + `Application/Auth/Commands/Register` + `Application/Account/Commands`) :
  - `GET  /api/v1/collection/value` → `CollectionValueDto` (somme meilleur prix marché des cartes possédées)
  - `GET  /api/v1/shop/products` (catalogue filtré `IsAvailable && Quantity>0`)
  - `POST /api/v1/shop/products/{id}/request-purchase`
  - `GET  /api/v1/shop/appointments/available-slots` (calcul créneaux depuis `Shop.HoursJson`)
  - `POST /api/v1/shop/appointments`
  - `GET  /api/v1/shop/appointments/mine`
  - `DELETE /api/v1/shop/appointments/{id}/cancel` (Pending + >24h)
  - `GET  /api/v1/admin/shop/purchase-requests` + `PUT .../{id}/status` (rôle admin)
  - `POST /api/v1/auth/register` (public, crée un compte + rôle `client`, retourne un `LoginResponse`)
  - `POST /api/v1/account/change-password`, `POST /api/v1/account/change-email`, `DELETE /api/v1/account`
- **Controllers** : `ShopClientController` (`[Authorize(Roles="client,admin")]`),
  `AdminShopPurchaseRequestsController` (`[Authorize(Roles="admin")]`), `AccountController`, `register` sur `AuthController`.
- **DI** : `IPurchaseRequestRepository` + `IShopClientRepository` enregistrés dans `Infrastructure/DependencyInjection.cs`.

### Frontend (Angular)
- `core/guards/client.guard.ts` — protège `/client/...`, redirige `/auth/login`, rôles `client`|`admin`.
- `features/client/layout/client-layout.component.ts` — navbar simplifiée (aucun menu admin).
- `features/client/` : `client.service.ts`, `client.models.ts`, `client.routes.ts` + 5 pages
  (collection, boutique + modale demande d'achat, rdv/new, rdv, profil).
- `features/auth/register/register.component.ts` (inscription publique).
- Routes `/auth/login`, `/auth/register`, `/client/...` ajoutées à `app.routes.ts`.
- `AuthStore` : ajout de `register` + redirection post-login selon le rôle (`admin`→`/dashboard`, sinon `/client/collection`).

## Point d'intégration avec le module Boutique (le « seam »)

Toute la dépendance concrète aux entités Boutique est **isolée dans un seul fichier** :
`Infrastructure/Persistence/Repositories/ShopClientRepository.cs`.

Aligné sur les entités Boutique réelles (déjà présentes) :
- `Shop.HoursJson` (string JSON des horaires).
- `ShopProduct` : `Type`, `CardId?`, `ProductName?`, `Condition?`, `GradingCompany?`, `GradeValue?`, `Price`,
  `Currency`, `Quantity`, `IsAvailable`, `PhotoUrl?`.
- `ShopAppointment.Create(shopId, clientName, clientEmail, clientPhone, serviceType, scheduledAt,
  durationMinutes, AppointmentStatus.Pending, notesClient, notesInternal, assignedToUserId)` et `ChangeStatus(...)`.

> Note : `ShopAppointment` n'a pas de `UserId`. L'appartenance d'un RDV à un client est déterminée par
> `ClientEmail` (copié depuis le compte à la création). Si le module Boutique ajoute plus tard un `UserId`,
> basculer `GetAppointmentsByEmailPagedAsync` / `GetAppointmentOwnershipAsync` sur ce champ.

## Actions dev requises

1. **Migration EF Core** (jamais générée manuellement) :
   ```
   dotnet ef migrations add AddClientModule
   dotnet ef database update
   ```
   (crée la table `purchase_request`).
2. **Seed du rôle système `client`** en base (via `Role.CreateSystemRole("client", "Client")`) — sinon
   `register` crée le compte mais sans rôle, et le `ClientGuard`/`[Authorize(Roles="client")]` refusera l'accès.
3. **Vérifier la compilation** : `dotnet build` (backend) et `ng build` (frontend). Le sandbox de génération
   n'a pas `dotnet`, et un `tsc` brut y produit des erreurs d'encodage parasites — la vérification finale
   se fait avec la vraie toolchain.

## Contraintes respectées
Aucun paiement en ligne. `UserId` extrait du JWT sur chaque action. Nom/email dénormalisés depuis `User`.
Aucune route `/client/...` accessible sans authentification.
