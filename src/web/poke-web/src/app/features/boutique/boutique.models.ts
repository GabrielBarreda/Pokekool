// ── Enums (miroir des enums backend, sérialisés en PascalCase) ──────────────
export type ShopProductType = 'CardRaw' | 'CardGraded' | 'SealedSet' | 'SealedUpc';
export type AppointmentStatus = 'Pending' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled';
export type ShopServiceType = 'PreGrading' | 'Restoration' | 'Purchase' | 'Other';

export const PRODUCT_TYPE_LABELS: Record<ShopProductType, string> = {
  CardRaw: 'Carte brute',
  CardGraded: 'Carte gradée',
  SealedSet: 'Scellé (set)',
  SealedUpc: 'Scellé (UPC)',
};

export const STATUS_LABELS: Record<AppointmentStatus, string> = {
  Pending: 'En attente',
  Confirmed: 'Confirmé',
  InProgress: 'En cours',
  Completed: 'Terminé',
  Cancelled: 'Annulé',
};

export const SERVICE_LABELS: Record<ShopServiceType, string> = {
  PreGrading: 'Pré-grading',
  Restoration: 'Restauration',
  Purchase: 'Rachat',
  Other: 'Autre',
};

// ── Pagination ──────────────────────────────────────────────────────────────
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// ── Shop info ────────────────────────────────────────────────────────────────
export interface DayHours {
  open: string;
  close: string;
  closed: boolean;
}
export type WeekHours = Record<string, DayHours>;

export interface ShopDto {
  id: string;
  name: string;
  description: string | null;
  address: string | null;
  city: string | null;
  zipCode: string | null;
  googleMapsUrl: string | null;
  hoursJson: string | null;
  services: string[];
  instagramUrl: string | null;
  tiktokUrl: string | null;
  photoUrls: string[];
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateShopInfoRequest {
  name: string;
  description: string | null;
  address: string | null;
  city: string | null;
  zipCode: string | null;
  googleMapsUrl: string | null;
  hoursJson: string | null;
  services: string[];
  instagramUrl: string | null;
  tiktokUrl: string | null;
  photoUrls: string[];
}

// ── Products (présentoir) ─────────────────────────────────────────────────────
export interface ShopProductDto {
  id: string;
  shopId: string;
  type: ShopProductType;
  cardId: string | null;
  cardName: string | null;
  cardImage: string | null;
  productName: string | null;
  condition: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  price: number;
  currency: string;
  quantity: number;
  isAvailable: boolean;
  description: string | null;
  photoUrl: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface ShopProductRequest {
  type: ShopProductType;
  cardId: string | null;
  productName: string | null;
  condition: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  price: number;
  currency: string;
  quantity: number;
  isAvailable: boolean;
  description: string | null;
  photoUrl: string | null;
}

export interface ProductFilters {
  type?: ShopProductType | null;
  isAvailable?: boolean | null;
  search?: string | null;
  page: number;
  pageSize: number;
}

// ── Stock ─────────────────────────────────────────────────────────────────────
export interface ShopStockDto {
  id: string;
  shopId: string;
  cardId: string | null;
  cardName: string | null;
  cardImage: string | null;
  productName: string | null;
  type: ShopProductType;
  condition: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  quantityTotal: number;
  quantityAvailable: number;
  quantityReserved: number;
  purchasePrice: number | null;
  purchaseCurrency: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface ShopStockRequest {
  cardId: string | null;
  productName: string | null;
  type: ShopProductType;
  condition: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  quantityTotal: number;
  quantityAvailable: number;
  quantityReserved: number;
  purchasePrice: number | null;
  purchaseCurrency: string | null;
  notes: string | null;
}

export interface StockFilters {
  type?: ShopProductType | null;
  search?: string | null;
  page: number;
  pageSize: number;
}

// ── Appointments (planning) ────────────────────────────────────────────────────
export interface ShopAppointmentDto {
  id: string;
  shopId: string;
  clientName: string;
  clientEmail: string;
  clientPhone: string | null;
  serviceType: ShopServiceType;
  scheduledAt: string;
  durationMinutes: number;
  status: AppointmentStatus;
  notesClient: string | null;
  notesInternal: string | null;
  assignedToUserId: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateAppointmentRequest {
  scheduledAt: string;
  durationMinutes: number;
  serviceType: ShopServiceType;
  notesInternal: string | null;
  assignedToUserId: string | null;
}

export interface AppointmentFilters {
  status?: AppointmentStatus | null;
  serviceType?: ShopServiceType | null;
  from?: string | null;
  to?: string | null;
}

// ── Card autocomplete (réutilise l'endpoint pokemon/cards) ─────────────────────
export interface CardSummary {
  id: string;
  name: string;
  imageSmall: string | null;
  setName: string;
}
