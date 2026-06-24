export interface NavItem {
  label: string;
  route: string;
  icon: string;
  permission?: string; // si absent = accessible à tous les authentifiés
  adminOnly?: boolean;
}

// Items visibles par tous les utilisateurs connectés
export const MAIN_NAV_ITEMS: NavItem[] = [
  { label: 'Tableau de bord', route: '/dashboard',  icon: '⊞' },
  { label: 'Bourse',          route: '/bourse',     icon: '🃏', permission: 'cards:read' },
  { label: 'Collection',      route: '/collection', icon: '📦' },
  { label: 'Messages',        route: '/messages',   icon: '💬' },
  { label: 'Mon profil',      route: '/profil',     icon: '👤' },
];

// Items visibles uniquement par les admins
export const ADMIN_ONLY_NAV_ITEMS: NavItem[] = [
  { label: 'Utilisateurs', route: '/utilisateurs', icon: '👥', adminOnly: true, permission: 'users:read' },
  { label: 'Rôles & perms', route: '/roles',       icon: '🔑', adminOnly: true, permission: 'roles:read' },
  { label: 'Paramètres',    route: '/settings',    icon: '⚙️', adminOnly: true },
];

// Alias pour la sidebar (rétrocompatibilité)
export const ADMIN_NAV_ITEMS = MAIN_NAV_ITEMS;
export const PLANNING_NAV_ITEMS: NavItem[] = [];
