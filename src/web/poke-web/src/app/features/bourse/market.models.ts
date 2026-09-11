// ── DTOs API ─────────────────────────────────────────────────────────────────

export interface SetFiltersDto {
  rarities: string[];
  supertypes: string[];
}


export interface SyncCatalogResultDto {
  seriesImported: number;
  setsImported: number;
  cardsImported: number;
  cardsUpdated: number;
  pricesUpserted: number;
  errors: string[] | null;
}

export interface SyncPricesResultDto {
  pricesUpserted: number;
  cardsFailed: number;
  errors: string[] | null;
  gradedByCompany?: Record<string, number> | null;
  cardsWithoutAnyPrice?: number;
  cardsWithKeyGradesMissing?: number;
}

export interface SyncPopulationResultDto {
  populationsUpserted: number;
  cardsFailed: number;
  errors: string[] | null;
}

export interface MarketCardDto {
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
  priceMarket: number | null;
  priceLow: number | null;
  priceCurrency: string | null;
  priceSource: string | null;
  alertCount: number;
}

export interface PokemonSetDto {
  id: string;
  tcgId: string;
  name: string;
  series: string;
  releaseDate: string | null;
  symbolUrl: string | null;
  logoUrl: string | null;
  totalCards: number | null;   // total TCG (inclut secret rares, alt arts…)
  printedTotal: number | null; // nombre imprimé sur la carte physique
  cardCount: number;           // cartes importées en base
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// ── Grading ───────────────────────────────────────────────────────────────────

export type GradingCompanyId = 'PSA' | 'BGS' | 'CGC' | 'SGC' | 'AGS' | 'CSG';

export interface GradingCompany {
  id: GradingCompanyId;
  name: string;
  color: string;       // couleur de la marque pour le graphe
  currency: string;
}

export interface GradeEntry {
  grade: number;       // 1 à 10
  label: string;       // '10', '9.5', '9', ... selon la société
  marketPrice: number | null;
  lowPrice: number | null;
  population: number | null;   // nb d'exemplaires gradés
}

export interface CardGradingData {
  cardId: string;
  company: GradingCompanyId;
  entries: GradeEntry[];
  lastUpdated: Date;
}

// ── DTO depuis l'API grading ──────────────────────────────────────────────────
export interface CardGradingApiDto {
  company: string;
  lastUpdated: string;
  entries: GradeEntryApiDto[];
}

export interface GradeEntryApiDto {
  grade: number;
  label: string;
  marketPrice: number | null;
  lowPrice: number | null;
  population: number | null;
}

// ── Sync planifié ────────────────────────────────────────────────────────────

export interface SyncScheduleDto {
  scheduledTime: string | null;      // "HH:mm"
  isRecurring: boolean;
  nextRunAt: string | null;          // ISO UTC
  status: 'none' | 'pending' | 'running' | 'completed' | 'cancelled';
  lastCompletedAt: string | null;
  lastPricesUpserted: number | null;
}

export interface SyncProgressEvent {
  setName: string;
  cardName: string;
  cardNumber: string;
  grades: string[];     // ["PSA7","PSA8","PSA9","PSA10"]
  success: boolean;
  timestamp: string;
  syncType: 'prices' | 'population' | 'catalog' | 'embeddings';  // type de sync en cours
  totalItems?: number;  // nb total de cartes (envoyé sur le 1er événement)
}

// ── Filtres marché ────────────────────────────────────────────────────────────

export interface MarketFilters {
  search: string;
  rarity: string;
  supertype: string;
  page: number;
  pageSize: number;
}
