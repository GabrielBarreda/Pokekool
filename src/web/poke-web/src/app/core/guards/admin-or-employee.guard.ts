import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';
import { PermissionStore } from '@core/auth/permission.store';
import { ToastService } from '@shared/services/toast.service';

/**
 * Autorise Admin, Super Admin et Employee sur le module Boutique.
 *
 * Le rôle "employee" n'existe pas encore en base — on le déclare quand même ici
 * pour préparer le terrain. Tant qu'il est absent, seuls les admins passent
 * (fallback), ce qui n'introduit aucune régression.
 *
 * @param adminOnly  true pour les pages réservées Admin (ex: Infos Boutique).
 */
export const adminOrEmployeeGuard = (adminOnly = false): CanActivateFn => {
  return () => {
    const authStore = inject(AuthStore);
    const permStore = inject(PermissionStore);
    const router = inject(Router);
    const toast = inject(ToastService);

    const roles = authStore.user()?.roles ?? [];
    const isAdmin = roles.includes('admin') || roles.includes('super_admin');
    const isEmployee = roles.includes('employee');

    // Défense en profondeur : on s'appuie aussi sur les permissions résolues côté API.
    const hasShopManage = permStore.hasPermission()('shop:manage');
    const hasShopRead = permStore.hasPermission()('shop:read');

    const allowed = adminOnly
      ? (isAdmin || hasShopManage)
      : (isAdmin || isEmployee || hasShopRead);

    if (allowed) return true;

    toast.show({ message: 'Accès refusé : réservé à l’équipe boutique', type: 'danger' });
    return router.createUrlTree(['/dashboard']);
  };
};
