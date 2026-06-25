import { Routes } from '@angular/router';

export const COLLECTION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./collection-page/collection-page.component').then(m => m.CollectionPageComponent),
    title: 'Ma Collection — Pokekool',
  },
];
