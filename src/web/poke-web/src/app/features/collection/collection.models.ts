// ── API DTOs ──────────────────────────────────────────────────────────────────

export interface CollectionCardDto {
  id: string;
  tcgId: string;
  number: string;
  name: string;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  supertype: string | null;
  rarity: string | null;
  types: string[] | null;
  imageSmall: string | null;
  imageLarge: string | null;
  setId: string;
  setName: string;
  setSymbolUrl: string | null;
  releaseYear: number | null;
  isOwned: boolean;
  isFavorite: boolean;
  isPriority: boolean;
}

export interface CollectionCardStatusDto {
  cardId: string;
  isOwned: boolean;
  isFavorite: boolean;
  isPriority: boolean;
}

export interface CollectionStatsDto {
  totalCards: number;
  ownedCards: number;
  favoriteCards: number;
  priorityCards: number;
  completionPercent: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// ── Local state types ─────────────────────────────────────────────────────────

export interface CollectionFilters {
  search: string;
  setId: string | null;
  rarity: string | null;
  year: number | null;
  ownedOnly: boolean;
  favoritesOnly: boolean;
  sortBy: 'name_asc' | 'name_desc' | 'rarity' | 'number';
  page: number;
  pageSize: number;
}

export interface SetOption {
  id: string;
  name: string;
  year: number | null;
}

export type ContextMenuAction = 'favorite' | 'priority' | 'create-deck';

export interface ContextMenuEvent {
  card: CollectionCardDto;
  x: number;
  y: number;
}

// ── Scan ──────────────────────────────────────────────────────
export interface CardScanMatchDto {
  cardId: string | null;
  cardName: string | null;
  /** Nom traduit selon la langue d'affichage préférée du client (résolu serveur). */
  displayName: string | null;
  setName: string | null;
  cardNumber: string | null;
  imageSmall: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  certNumber: string | null;
  confidence: number;
  matched: boolean;
}

export interface ScanCollectionResult {
  matches: CardScanMatchDto[];
}

export interface BulkAddResult {
  added: number;
  alreadyOwned: number;
}
