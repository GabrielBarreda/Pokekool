namespace Pokekool.Shared.Dtos.Dashboard;

public record DashboardSummaryDto(
    // ── Bourse ──────────────────────────────────────────────
    IReadOnlyList<RecentlyScrapedCardDto> RecentlyScrapedCards,
    IReadOnlyList<ForSaleCardDto>         CardsForSale,
    // ── Collection ──────────────────────────────────────────
    int                                   TotalUsersCollecting,
    // ── Ventes ──────────────────────────────────────────────
    IReadOnlyList<RecentSaleDto>          RecentSales,
    // ── Messagerie ──────────────────────────────────────────
    int                                   PendingMessagesCount,
    // ── IA ──────────────────────────────────────────────────
    AiStatsDto                            AiStats
);

public record RecentlyScrapedCardDto(
    string   CardId,
    string?  TcgId,
    string   Name,
    string   DisplayName,
    string?  ImageSmall,
    string   SetName,
    string?  Rarity,
    decimal? PriceMarket,
    string?  PriceCurrency,
    string?  PriceSource,
    DateTime ScrapedAt
);

public record ForSaleCardDto(
    string   CardId,
    string?  TcgId,
    string   Name,
    string   DisplayName,
    string?  ImageSmall,
    decimal  SalePrice,
    string   Currency,
    int      InterestedCount,
    TimeSpan ForSaleSince
);

public record RecentSaleDto(
    string   CardId,
    string   Name,
    string?  ImageSmall,
    string   SetName,
    decimal  SalePrice,
    string   Currency,
    string?  GradeCompany,
    decimal? GradeValue,
    DateTime SoldAt
);

public record AiStatsDto(
    int  MessagesAnsweredToday,
    int  MessagesAnsweredThisWeek,
    bool IsEnabled
);
