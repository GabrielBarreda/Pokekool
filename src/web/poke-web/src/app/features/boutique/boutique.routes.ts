import { Routes } from '@angular/router';
import { adminOrEmployeeGuard } from '@core/guards/admin-or-employee.guard';

export const BOUTIQUE_ROUTES: Routes = [
  { path: '', redirectTo: 'presentoir', pathMatch: 'full' },
  {
    path: 'infos',
    canActivate: [adminOrEmployeeGuard(true)],
    loadComponent: () =>
      import('./pages/shop-info/shop-info.component').then(m => m.ShopInfoComponent),
    title: 'Infos boutique — Pokekool',
  },
  {
    path: 'presentoir',
    canActivate: [adminOrEmployeeGuard()],
    loadComponent: () =>
      import('./pages/presentoir/presentoir.component').then(m => m.PresentoirComponent),
    title: 'Présentoir — Pokekool',
  },
  {
    path: 'stock',
    canActivate: [adminOrEmployeeGuard()],
    loadComponent: () =>
      import('./pages/stock/stock.component').then(m => m.StockComponent),
    title: 'Stock boutique — Pokekool',
  },
  {
    path: 'planning',
    canActivate: [adminOrEmployeeGuard()],
    loadComponent: () =>
      import('./pages/planning/planning.component').then(m => m.PlanningComponent),
    title: 'Planning RDV — Pokekool',
  },
  {
    path: 'stockage',
    canActivate: [adminOrEmployeeGuard()],
    loadComponent: () =>
      import('./stockage/stockage.component').then(m => m.StockageComponent),
    title: 'Stockage physique — Pokekool',
  },
];
