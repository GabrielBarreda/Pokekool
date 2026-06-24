using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

public sealed class PokemonSeries : Entity
{
    public string TcgId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? LogoUrl { get; private set; }

    private readonly List<PokemonSet> _sets = [];
    public IReadOnlyCollection<PokemonSet> Sets => _sets.AsReadOnly();

    private PokemonSeries() { }

    public static PokemonSeries Create(string tcgId, string name, string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(tcgId))
            throw new ArgumentException("TcgId is required", nameof(tcgId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new PokemonSeries
        {
            Id = Guid.NewGuid(),
            TcgId = tcgId.Trim(),
            Name = name.Trim(),
            LogoUrl = logoUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        Name = name.Trim();
        LogoUrl = logoUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
