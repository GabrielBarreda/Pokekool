using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Dashboard;

namespace Pokekool.Application.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IDashboardRepository _dashboardRepo;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetDashboardSummaryQueryHandler(
        IDashboardRepository dashboardRepo,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _dashboardRepo = dashboardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // Les deux requêtes utilisent le même DbContext (scoped) — EF Core n'autorise pas
        // de concurrence sur une même instance, on les exécute séquentiellement.
        var scraped          = await _dashboardRepo.GetRecentlyScrapedCardsAsync(cancellationToken);
        var usersCollecting  = await _dashboardRepo.GetTotalUsersCollectingAsync(cancellationToken);

        // CardId est un string ici (projection dashboard) : seules les entrées qui parsent en
        // Guid valide sont résolvables en base pokemon_cards pour la traduction ; les autres
        // gardent DisplayName == Name (repli, cas normalement inexistant en pratique).
        var englishNamesByCardId = new Dictionary<Guid, string>();
        foreach (var p in scraped)
        {
            if (Guid.TryParse(p.CardId, out var cardGuid))
                englishNamesByCardId[cardGuid] = p.Name;
        }
        var displayNames = await _displayNameResolver.ResolveManyAsync(
            englishNamesByCardId, _displayLanguage.Language, cancellationToken);

        var recentlyScrapedDtos = scraped
            .Select(p => new RecentlyScrapedCardDto(
                p.CardId,
                p.TcgId,
                p.Name,
                Guid.TryParse(p.CardId, out var cardGuid) && displayNames.TryGetValue(cardGuid, out var displayName)
                    ? displayName
                    : p.Name,
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
