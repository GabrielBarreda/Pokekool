import { Routes } from '@angular/router';
import { permissionGuard } from '@core/guards/permission.guard';

export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./establishments/establishments-list/establishments-list.component').then(
        m => m.EstablishmentsListComponent
      ),
    canActivate: [permissionGuard('establishments.read')],
    title: 'Établissements — Pokekool',
  },
];
