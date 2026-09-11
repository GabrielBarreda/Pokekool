namespace Pokekool.Application.Pokemon.Services;

/// <summary>
/// Résout DisplayName pour un ensemble de cartes, dans la langue demandée. Centralise la règle
/// de repli (pas de traduction connue, ou langue = "en" = la langue de référence de
/// PokemonCard.Name → DisplayName == Name) pour éviter de la dupliquer dans chaque handler/mapper
/// (voir ADR-004, mémoire Obsidian du projet).
/// </summary>
public interface ICardDisplayNameResolver
{
    /// <summary>
    /// Résout le nom d'affichage d'une seule carte. Pratique pour les handlers qui construisent
    /// un DTO unitaire (GetCardById, etc.) — pour un lot, préférer <see cref="ResolveManyAsync"/>
    /// (une requête au lieu de N).
    /// </summary>
    Task<string> ResolveAsync(Guid cardId, string englishName, string language, CancellationToken ct = default);

    /// <summary>
    /// Résout le nom d'affichage de plusieurs cartes en une seule requête. Le dictionnaire
    /// <paramref name="englishNamesByCardId"/> sert à la fois de liste des cartes à résoudre et
    /// de valeur de repli. Retourne un dictionnaire complet (une entrée par carte d'entrée).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolveManyAsync(
        IReadOnlyDictionary<Guid, string> englishNamesByCardId, string language, CancellationToken ct = default);
}
