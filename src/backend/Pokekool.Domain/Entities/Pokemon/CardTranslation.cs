using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Pokemon;

/// <summary>
/// Traduction du nom d'une carte dans une langue d'affichage donnée (UI uniquement — voir
/// ADR-004, mémoire Obsidian du projet). N'affecte jamais <see cref="PokemonCard.Name"/>, qui
/// reste la seule clé utilisée par le matching texte, le scan, la recherche et les exports.
/// Une carte peut avoir 0..n traductions (une par langue) ; l'absence de ligne pour une langue
/// donnée signifie repli sur le nom anglais côté résolution (voir CardTranslationRepository).
/// </summary>
public sealed class CardTranslation : Entity
{
    public Guid CardId { get; private set; }
    public string Language { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    // Navigation
    public PokemonCard? Card { get; private set; }

    private CardTranslation() { }

    public static CardTranslation Create(Guid cardId, string language, string name)
    {
        if (cardId == Guid.Empty)
            throw new ArgumentException("CardId is required", nameof(cardId));
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language is required", nameof(language));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new CardTranslation
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Language = language.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
