namespace Pokekool.Application.Abstractions;

/// <summary>
/// Client pour l'API TCGdex (https://api.tcgdex.net) — source des noms de cartes traduits
/// (FR aujourd'hui, autres langues potentiellement plus tard). Utilisé uniquement pour
/// alimenter <c>card_translations</c> (affichage UI) ; ne touche à aucune donnée référentielle
/// (voir ADR-004, mémoire Obsidian du projet).
/// </summary>
public interface ITcgDexClient
{
    /// <summary>
    /// Retourne, pour un set donné, la correspondance TcgId (identifiant pokemontcg.io,
    /// identique à l'id TCGdex sur les sets modernes) -> nom localisé, pour la langue demandée.
    /// Un set introuvable côté TCGdex (ex. très ancien, absent de leur référentiel) retourne
    /// une liste vide plutôt que de lever — la carte reste alors en repli EN côté résolution.
    /// </summary>
    Task<IReadOnlyList<TcgDexCardNameDto>> GetCardNamesBySetAsync(
        string setId, string language, CancellationToken ct = default);
}

public sealed record TcgDexCardNameDto(string TcgId, string Name);
