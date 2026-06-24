import { Routes } from '@angular/router';

export const MARKET_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./market-list/market-list.component').then(m => m.MarketListComponent),
    title: 'Bourse — Pokekool',
  },
];
