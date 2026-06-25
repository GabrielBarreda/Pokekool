export interface UserSummaryDto {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  isActive: boolean;
  roles: string[];
  hasPendingMessages: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface UsersFilters {
  search: string;
  roleCode: string | null;
  hasPendingMessages: boolean | null;
  recentOnly: boolean | null;
  sortBy: 'name_asc' | 'name_desc' | 'email' | 'created_desc';
  page: number;
  pageSize: number;
}

export interface AvailableRole {
  code: string;
  name: string;
}

export const DEFAULT_FILTERS: UsersFilters = {
  search: '',
  roleCode: null,
  hasPendingMessages: null,
  recentOnly: null,
  sortBy: 'created_desc',
  page: 1,
  pageSize: 30,
};

/** Couleur de badge par code de rôle système */
export type RoleBadgeVariant = 'brand' | 'info' | 'neutral';

export function roleBadgeVariant(code: string): RoleBadgeVariant {
  if (code === 'super_admin') return 'brand'; // violet
  if (code === 'admin')       return 'info';  // bleu
  return 'neutral';                           // gris
}

export function userDisplayName(u: UserSummaryDto): string {
  const full = [u.firstName, u.lastName].filter(Boolean).join(' ');
  return full || u.email;
}

export function userInitials(u: UserSummaryDto): string {
  const f = u.firstName?.[0] ?? '';
  const l = u.lastName?.[0]  ?? '';
  return (f + l).toUpperCase() || u.email[0].toUpperCase();
}
