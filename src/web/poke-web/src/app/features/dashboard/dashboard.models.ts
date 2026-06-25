// ── Miroir des DTOs backend (camelCase) ──────────────────────────────────────

export interface RecentlyScrapedCardDto {
  cardId:        string;
  name:          string;
  imageSmall:    string | null;
  setName:       string;
  rarity:        string | null;
  priceMarket:   number | null;
  priceCurrency: string | null;
  priceSource:   string | null;
  scrapedAt:     string; // ISO UTC
}

export interface ForSaleCardDto {
  cardId:          string;
  name:            string;
  imageSmall:      string | null;
  salePrice:       number;
  currency:        string;
  interestedCount: number;
  forSaleSince:    string; // ISO duration ou datetime selon backend
}

export interface RecentSaleDto {
  cardId:       string;
  name:         string;
  imageSmall:   string | null;
  setName:      string;
  salePrice:    number;
  currency:     string;
  gradeCompany: string | null;
  gradeValue:   number | null;
  soldAt:       string; // ISO UTC
}

export interface AiStatsDto {
  messagesAnsweredToday:    number;
  messagesAnsweredThisWeek: number;
  isEnabled:                boolean;
}

export interface DashboardSummaryDto {
  recentlyScrapedCards: RecentlyScrapedCardDto[];
  cardsForSale:         ForSaleCardDto[];
  totalUsersCollecting: number;
  recentSales:          RecentSaleDto[];
  pendingMessagesCount: number;
  aiStats:              AiStatsDto;
}
