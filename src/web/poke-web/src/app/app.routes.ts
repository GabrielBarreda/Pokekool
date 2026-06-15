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

  // ── Protected admin shell ─────────────────────────────────
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

      // Plans de salle (Phase 2)
      {
        path: 'floor-plans',
        loadChildren: () =>
          import('./features/floor-plans/floor-plans.routes').then(m => m.FLOOR_PLAN_ROUTES),
        title: 'Plans de salle — Pokekool',
      },

      // Réservations (Phase 2)
      {
        path: 'reservations',
        loadChildren: () =>
          import('./features/reservations/reservations.routes').then(m => m.RESERVATION_ROUTES),
        title: 'Réservations — Pokekool',
      },

      // Commandes (Phase 2)
      {
        path: 'orders',
        loadChildren: () =>
          import('./features/orders/orders.routes').then(m => m.ORDER_ROUTES),
        title: 'Commandes — Pokekool',
      },

      // Menus
      {
        path: 'menus',
        loadChildren: () =>
          import('./features/menus/menus.routes').then(m => m.MENU_ROUTES),
        title: 'Menus — Pokekool',
      },

      // Équipe (admin only — phase 1)
      {
        path: 'team',
        loadChildren: () =>
          import('./features/team/team.routes').then(m => m.TEAM_ROUTES),
        title: 'Équipe — Pokekool',
      },

      // Rôles & permissions (admin only — phase 1)
      {
        path: 'roles',
        loadChildren: () =>
          import('./features/roles/roles.routes').then(m => m.ROLE_ROUTES),
        title: 'Rôles — Pokekool',
      },

      // Planning
      {
        path: 'planning',
        loadChildren: () =>
          import('./features/planning/planning.routes').then(m => m.PLANNING_ROUTES),
        title: 'Planning — Pokekool',
      },

      // Paramètres (admin only — phase 1)
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
