import { Routes } from '@angular/router';

export const CLIENT_ROUTES: Routes = [
  {
    path: 'collection',
    loadComponent: () =>
      import('./pages/collection/client-collection.component').then(m => m.ClientCollectionComponent),
    title: 'Ma Collection — Pokekool',
  },
  {
    path: 'boutique',
    loadComponent: () =>
      import('./pages/boutique/client-boutique.component').then(m => m.ClientBoutiqueComponent),
    title: 'Boutique — Pokekool',
  },
  {
    path: 'rdv/new',
    loadComponent: () =>
      import('./pages/rdv-new/rdv-new.component').then(m => m.RdvNewComponent),
    title: 'Prendre un RDV — Pokekool',
  },
  {
    path: 'rdv',
    loadComponent: () =>
      import('./pages/rdv/rdv-list.component').then(m => m.RdvListComponent),
    title: 'Mes réservations — Pokekool',
  },
  {
    path: 'profil',
    loadComponent: () =>
      import('./pages/profil/client-profil.component').then(m => m.ClientProfilComponent),
    title: 'Mon profil — Pokekool',
  },
  { path: '', redirectTo: 'collection', pathMatch: 'full' },
];
