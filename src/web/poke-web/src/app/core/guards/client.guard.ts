import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';

/**
 * Protège toutes les routes /client/... :
 *  - JWT absent / expiré → redirect /auth/login
 *  - rôle autorisé : "client" ou "admin"
 */
export const clientGuard: CanActivateFn = () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (!authStore.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }

  const roles = authStore.user()?.roles ?? [];
  const allowed = roles.includes('client') || roles.includes('admin');

  return allowed ? true : router.createUrlTree(['/auth/login']);
};
