import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionStore } from '@core/auth/permission.store';
import { ToastService } from '@shared/services/toast.service';

/**
 * Usage in routes:
 * canActivate: [permissionGuard('menus.write')]
 */
export const permissionGuard = (requiredPermission: string): CanActivateFn => {
  return () => {
    const permStore = inject(PermissionStore);
    const router = inject(Router);
    const toast = inject(ToastService);

    if (permStore.hasPermission()(requiredPermission)) return true;

    toast.show({ message: 'Accès refusé : permission insuffisante', type: 'danger' });
    return router.createUrlTree(['/dashboard']);
  };
};
