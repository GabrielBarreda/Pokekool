// ── Pagination ────────────────────────────────────────────────
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// ── Collection ────────────────────────────────────────────────
export interface CollectionValueDto {
  totalValue: number;
  currency: string;
  cardCount: number;
}

// ── Boutique ──────────────────────────────────────────────────
export interface ShopProductDto {
  id: string;
  shopId: string;
  type: string;
  cardId?: string | null;
  cardTcgId?: string | null;
  cardName?: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName?: string | null;
  cardImage?: string | null;
  productName?: string | null;
  condition?: string | null;
  gradingCompany?: string | null;
  gradeValue?: number | null;
  price: number;
  currency: string;
  quantity: number;
  isAvailable: boolean;
  description?: string | null;
  photoUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface ShopProductFilters {
  type?: string;
  gradeMin?: number;
  gradeMax?: number;
  priceMax?: number;
  search?: string;
  page: number;
  pageSize: number;
}

// ── RDV / Appointments ────────────────────────────────────────
export type ClientServiceType = 'pre_grading' | 'restoration';

export interface AvailableSlotDto {
  date: string; // YYYY-MM-DD
  time: string; // HH:mm
  available: boolean;
}

export interface ShopAppointmentDto {
  id: string;
  shopId: string;
  clientName: string;
  clientEmail: string;
  clientPhone?: string | null;
  serviceType: string;
  scheduledAt: string;
  durationMinutes: number;
  status: string;
  notesClient?: string | null;
  notesInternal?: string | null;
  assignedToUserId?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateAppointmentBody {
  serviceType: ClientServiceType;
  scheduledAt: string; // ISO
  notesClient?: string | null;
}
