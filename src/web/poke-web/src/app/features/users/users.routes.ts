import { Routes } from '@angular/router';
import { permissionGuard } from '@core/guards/permission.guard';

export const USERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./users-list/users-list.component').then(m => m.UsersListComponent),
    canActivate: [permissionGuard('users:read')],
    title: 'Utilisateurs — Pokekool',
  },
];
