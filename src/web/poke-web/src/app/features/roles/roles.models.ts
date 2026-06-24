export interface RoleDto {
  id: string;
  tenantId: string | null;
  code: string;
  name: string;
  description: string | null;
  isSystem: boolean;
  permissions: string[];
  createdAt: string;
  updatedAt: string | null;
}

export interface PermissionDto {
  code: string;
  category: string;
  description: string;
}

export interface CreateRoleRequest {
  code: string;
  name: string;
  description: string | null;
  permissions: string[];
}

export interface UpdateRoleRequest {
  name: string;
  description: string | null;
  permissions: string[];
}

/** Groupes de permissions par catégorie pour l'UI */
export interface PermissionGroup {
  category: string;
  label: string;
  permissions: PermissionDto[];
}

const CATEGORY_LABELS: Record<string, string> = {
  cards: 'Cartes Pokémon',
  users: 'Utilisateurs',
  roles: 'Rôles & permissions',
  collection: 'Collection',
  alerts: 'Alertes',
  messages: 'Messagerie',
};

export function groupPermissions(perms: PermissionDto[]): PermissionGroup[] {
  const map = new Map<string, PermissionDto[]>();
  for (const p of perms) {
    if (!map.has(p.category)) map.set(p.category, []);
    map.get(p.category)!.push(p);
  }
  return Array.from(map.entries()).map(([category, permissions]) => ({
    category,
    label: CATEGORY_LABELS[category] ?? category,
    permissions,
  }));
}
