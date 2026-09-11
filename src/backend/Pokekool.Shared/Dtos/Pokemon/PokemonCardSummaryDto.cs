namespace Pokekool.Shared.Dtos.Pokemon;

public sealed record PokemonCardSummaryDto(
    Guid Id,
    string TcgId,
    string Number,
    string Name,
    string DisplayName,
    string? Rarity,
    string? ImageSmall,
    string SetName,
    string SetTcgId);
