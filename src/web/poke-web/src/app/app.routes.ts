import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';

export const routes: Routes = [
  // ── Public routes ─────────────────────────────────────────
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent),
    title: 'Connexion — Pokekool',
  },

  // ── Protected shell ───────────────────────────────────────
  {
    path: '',
    loadComponent: () =>
      import('./layout/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      // Dashboard
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        title: 'Tableau de bord — Pokekool',
      },

      // Bourse (Phase 1)
      {
        path: 'bourse',
        loadChildren: () =>
          import('./features/bourse/market.routes').then(m => m.MARKET_ROUTES),
      },

      // Collection (Phase 2)
      {
        path: 'collection',
        loadChildren: () =>
          import('./features/collection/collection.routes').then(m => m.COLLECTION_ROUTES),
      },

      // Messages (Phase 2)
      {
        path: 'messages',
        loadComponent: () =>
          import('./features/coming-soon/coming-soon.component').then(m => m.ComingSoonComponent),
        title: 'Messages — Pokekool',
        data: { feature: 'Messages' },
      },

      // Profil
      {
        path: 'profil',
        loadComponent: () =>
          import('./features/coming-soon/coming-soon.component').then(m => m.ComingSoonComponent),
        title: 'Mon profil — Pokekool',
        data: { feature: 'Profil' },
      },

      // ── Admin only ──────────────────────────────────────────

      // Utilisateurs (admin)
      {
        path: 'utilisateurs',
        loadChildren: () =>
          import('./features/users/users.routes').then(m => m.USERS_ROUTES),
      },

      // Rôles & permissions (admin)
      {
        path: 'roles',
        loadChildren: () =>
          import('./features/roles/roles.routes').then(m => m.ROLE_ROUTES),
        title: 'Rôles — Pokekool',
      },

      // Paramètres (admin)
      {
        path: 'settings',
        loadChildren: () =>
          import('./features/settings/settings.routes').then(m => m.SETTINGS_ROUTES),
        title: 'Paramètres — Pokekool',
      },

      // Default redirect
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },

  // ── Catch-all ─────────────────────────────────────────────
  { path: '**', redirectTo: '' },
];
