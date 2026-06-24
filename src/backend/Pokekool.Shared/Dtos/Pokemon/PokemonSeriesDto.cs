namespace Pokekool.Shared.Dtos.Pokemon;

public sealed record PokemonSeriesDto(
    Guid Id,
    string TcgId,
    string Name,
    string? LogoUrl,
    int SetCount);
