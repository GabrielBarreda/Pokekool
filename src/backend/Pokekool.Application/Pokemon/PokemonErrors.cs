using Pokekool.Domain.Common;

namespace Pokekool.Application.Pokemon;

public static class PokemonErrors
{
    public static DomainException SeriesNotFound(Guid id)
        => new("pokemon_series.not_found", $"Pokemon series '{id}' not found.");

    public static DomainException SetNotFound(Guid id)
        => new("pokemon_set.not_found", $"Pokemon set '{id}' not found.");

    public static DomainException CardNotFound(Guid id)
        => new("pokemon_card.not_found", $"Pokemon card '{id}' not found.");
}
