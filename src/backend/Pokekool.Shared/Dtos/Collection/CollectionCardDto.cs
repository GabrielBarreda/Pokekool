namespace Pokekool.Shared.Dtos.Collection;

public record CollectionCardDto(
    string Id,
    string TcgId,
    string Number,
    string Name,
    string? Supertype,
    string? Rarity,
    string[]? Types,
    string? ImageSmall,
    string? ImageLarge,
    string SetId,
    string SetName,
    string? SetSymbolUrl,
    int? ReleaseYear,
    bool IsOwned,
    bool IsFavorite,
    bool IsPriority
);
