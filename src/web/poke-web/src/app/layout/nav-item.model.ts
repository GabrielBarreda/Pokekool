export interface NavItem {
  label: string;
  route: string;
  icon: string;        // CSS class ou unicode
  permission?: string; // si null/undefined = accessible à tous les authentifiés
  adminOnly?: boolean; // restreint au rôle restaurant_admin
}

export const ADMIN_NAV_ITEMS: NavItem[] = [
  { label: 'Tableau de bord', route: '/dashboard',   icon: '⊞',  adminOnly: false },
  { label: 'Plans de salle',  route: '/floor-plans',  icon: '⬡',  adminOnly: false, permission: 'tables.read' },
  { label: 'Réservations',    route: '/reservations', icon: '📅', adminOnly: false, permission: 'reservations.read' },
  { label: 'Commandes',       route: '/orders',       icon: '🧾', adminOnly: false, permission: 'orders.read' },
  { label: 'Menus',           route: '/menus',        icon: '🍽', adminOnly: false, permission: 'menus.read' },
];

export const ADMIN_ONLY_NAV_ITEMS: NavItem[] = [
  { label: 'Équipe',          route: '/team',         icon: '👥', adminOnly: true,  permission: 'users.read' },
  { label: 'Rôles & perms',   route: '/roles',        icon: '🔑', adminOnly: true,  permission: 'roles.read' },
];

export const PLANNING_NAV_ITEMS: NavItem[] = [
  { label: 'Planning & Shifts', route: '/planning', icon: '📅', adminOnly: false }
];
