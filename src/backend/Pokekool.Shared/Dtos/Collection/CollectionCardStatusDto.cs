namespace Pokekool.Shared.Dtos.Collection;

public record CollectionCardStatusDto(
    string CardId,
    bool IsOwned,
    bool IsFavorite,
    bool IsPriority
);
