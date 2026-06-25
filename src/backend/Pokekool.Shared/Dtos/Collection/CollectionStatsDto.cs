namespace Pokekool.Shared.Dtos.Collection;

public record CollectionStatsDto(
    int TotalCards,
    int OwnedCards,
    int FavoriteCards,
    int PriorityCards,
    double CompletionPercent
);
