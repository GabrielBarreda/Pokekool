# Pokekool.Mobile — App .NET MAUI (Admin & Client)

App mobile complète (Prompt-14) : `AppShell` avec **TabBar conditionné par le rôle** décodé du JWT.

- `admin` / `super_admin` / `employee` → TabBar **Admin** : Pokémon (Bourse, Scan, Collection, À vendre) · Boutique (Présentoir, Stock, Planning, Stockage, Demandes, Infos, Assistant IA) · Paramètres (Profil, Utilisateurs, Rôles, Providers)
- `client` → TabBar **Client** : Boutique (Catalogue, RDV, Mes RDV, Messagerie*) · Collection (Ma collection, Scan, Alertes*)

\* placeholders « Bientôt disponible » — endpoints pas encore exposés (cf. Vault : Messagerie-Feature / Alertes-Feature).

## Architecture

- **MVVM** : `CommunityToolkit.Mvvm` (ObservableProperty / RelayCommand), DI via `MauiProgram`.
- **DTOs** : référence projet `Pokekool.Shared` — zéro duplication de contrats.
- **Auth** : `AuthService` (login/refresh/logout), access token en mémoire, refresh token en `SecureStorage` ; `AuthHttpMessageHandler` injecte le Bearer et rejoue 1× après refresh sur 401 (miroir de l'intercepteur Angular).
- **Scan** : `MediaPicker.CapturePhotoAsync` (caméra arrière) → `POST api/v1/collection/scan` (multipart) → sélection → `bulk-own`.
- **Design tokens** : `Resources/Styles/Tokens.xaml` généré par `npm run tokens:maui` — ne pas éditer à la main.

## Build (sur ton poste)

```powershell
dotnet workload install maui        # une seule fois
cd src/mobile/Pokekool.Mobile
dotnet build -f net8.0-android
dotnet build -t:Run -f net8.0-android   # émulateur / appareil branché
```

URL API (`Services/ApiConfig.cs`) : émulateur Android → `http://10.0.2.2:5000/` ; appareil physique → remplacer par l'IP LAN de la machine qui héberge l'API (et garder `usesCleartextTraffic` en dev uniquement).
