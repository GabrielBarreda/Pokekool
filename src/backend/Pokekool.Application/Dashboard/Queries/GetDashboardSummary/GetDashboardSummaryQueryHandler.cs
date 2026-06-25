using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Dashboard;

namespace Pokekool.Application.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IDashboardRepository _dashboardRepo;

    public GetDashboardSummaryQueryHandler(IDashboardRepository dashboardRepo)
    {
        _dashboardRepo = dashboardRepo;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // Les deux requêtes utilisent le même DbContext (scoped) — EF Core n'autorise pas
        // de concurrence sur une même instance, on les exécute séquentiellement.
        var scraped          = await _dashboardRepo.GetRecentlyScrapedCardsAsync(cancellationToken);
        var usersCollecting  = await _dashboardRepo.GetTotalUsersCollectingAsync(cancellationToken);

        var recentlyScrapedDtos = scraped
            .Select(p => new RecentlyScrapedCardDto(
                p.CardId,
                p.Name,
                p.ImageSmall,
                p.SetName,
                p.Rarity,
                p.PriceMarket,
                p.PriceCurrency,
                p.PriceSource,
                p.ScrapedAt))
            .ToList();

        return new DashboardSummaryDto(
            RecentlyScrapedCards: recentlyScrapedDtos,
            CardsForSale:         [],           // TODO: brancher quand CardSale implémenté
            TotalUsersCollecting: usersCollecting,
            RecentSales:          [],           // TODO: brancher quand CardSale implémenté
            PendingMessagesCount: 0,            // TODO: brancher quand Messaging implémenté
            AiStats: new AiStatsDto(
                MessagesAnsweredToday:    0,
                MessagesAnsweredThisWeek: 0,
                IsEnabled:               false  // feature flag — désactivé tant que non implémenté
            )
        );
    }
}
