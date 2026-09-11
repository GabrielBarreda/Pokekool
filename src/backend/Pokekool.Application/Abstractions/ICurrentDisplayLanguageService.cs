namespace Pokekool.Application.Abstractions;

/// <summary>
/// Expose la langue d'affichage préférée du client courant, transmise via le header HTTP
/// "X-Display-Language" (voir CurrentDisplayLanguageService). Utilisée pour résoudre
/// DisplayName sur les DTOs porteurs d'un nom de carte — jamais pour altérer PokemonCard.Name,
/// qui reste la clé anglaise stable (voir ADR-004, mémoire Obsidian du projet).
/// </summary>
public interface ICurrentDisplayLanguageService
{
    /// <summary>Toujours en minuscules, jamais vide (repli "en" — anglais = valeur de référence
    /// de PokemonCard.Name, donc "pas de traduction" et "langue = anglais" sont équivalents).</summary>
    string Language { get; }
}
