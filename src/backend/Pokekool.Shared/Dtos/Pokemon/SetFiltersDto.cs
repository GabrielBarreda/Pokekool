namespace Pokekool.Shared.Dtos.Pokemon;

/// <summary>
/// Valeurs de filtres disponibles pour un set donné (rarités et supertypes présents).
/// </summary>
public sealed record SetFiltersDto(
    IReadOnlyList<string> Rarities,
    IReadOnlyList<string> Supertypes);
