export interface NavItem {
  label: string;
  route: string;
  icon: string;
  permission?: string; // si absent = accessible à tous les authentifiés
  adminOnly?: boolean;
  children?: NavItem[]; // sous-menu (ex: Boutique)
}

// Items visibles par tous les utilisateurs connectés
export const MAIN_NAV_ITEMS: NavItem[] = [
  { label: 'Tableau de bord', route: '/dashboard',  icon: '⊞' },
  { label: 'Bourse',          route: '/bourse',     icon: '🃏', permission: 'cards:read' },
  { label: 'Collection',      route: '/collection', icon: '📦' },
  { label: 'Messages',        route: '/messages',   icon: '💬' },
  { label: 'Mon profil',      route: '/profil',     icon: '👤' },
];

// Items section Boutique (entre main et admin)
export const BOUTIQUE_NAV_ITEMS: NavItem[] = [
  {
    label: 'Boutique', route: '/boutique', icon: '', permission: 'shop:read',
    children: [
      { label: 'Infos',      route: '/boutique/infos',      icon: '🏪', adminOnly: true, permission: 'shop:manage' },
      { label: 'Présentoir', route: '/boutique/presentoir', icon: '🃏', permission: 'shop:read' },
      { label: 'Stock',      route: '/boutique/stock',      icon: '📦', permission: 'shop:read' },
      { label: 'Planning',   route: '/boutique/planning',   icon: '📅', permission: 'shop:read' },
      { label: 'Stockage',   route: '/boutique/stockage',   icon: '🏬', permission: 'storage:read' },
    ],
  },
];

// Items visibles uniquement par les admins
export const ADMIN_ONLY_NAV_ITEMS: NavItem[] = [
  { label: 'Utilisateurs',      route: '/utilisateurs',   icon: '👥', adminOnly: true, permission: 'users:read' },
  { label: 'Rôles & perms',     route: '/roles',          icon: '🔑', adminOnly: true, permission: 'roles:read' },
  { label: 'Paramètres',        route: '/settings',       icon: '⚙️', adminOnly: true },
  { label: 'Scan & Historique', route: '/scan-historique', icon: '🧪', adminOnly: true },
];

// Alias pour la sidebar (rétrocompatibilité)
export const ADMIN_NAV_ITEMS = MAIN_NAV_ITEMS;
export const PLANNING_NAV_ITEMS: NavItem[] = [];
