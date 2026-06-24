import { Routes } from '@angular/router';
import { permissionGuard } from '@core/guards/permission.guard';

export const ROLE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./roles-list/roles-list.component').then(m => m.RolesListComponent),
    canActivate: [permissionGuard('roles:read')],
    title: 'Rôles & permissions — Pokekool',
  },
];
