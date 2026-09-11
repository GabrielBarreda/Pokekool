namespace Pokekool.Shared.Dtos.Pokemon;

public sealed record PokemonCardDto(
    Guid Id,
    string TcgId,
    string Number,
    string Name,
    string DisplayName,
    string? Supertype,
    string[]? Subtypes,
    string? Rarity,
    string[]? Types,
    int? Hp,
    string? ImageSmall,
    string? ImageLarge,
    string SetName,
    string? SetSymbolUrl);
