namespace Pokekool.Shared.Dtos.Pokemon;

public sealed record PokemonSetDto(
    Guid Id,
    string TcgId,
    string Name,
    string? Series,
    DateOnly? ReleaseDate,
    int? TotalCards,
    int? PrintedTotal,
    string? SymbolUrl,
    string? LogoUrl,
    int CardCount);
