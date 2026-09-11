using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface ICardTranslationRepository
{
    /// <summary>
    /// Ids des cartes d'un set n'ayant pas encore de traduction pour la langue donnée —
    /// utilisé par le sync pour ne (re)traiter que ce qui manque (skip par carte, pas par set,
    /// voir SyncCardTranslationsCommandHandler).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetCardIdsWithoutTranslationAsync(
        Guid setId, string language, CancellationToken ct = default);

    /// <summary>Upsert en masse (une carte = au plus une ligne par langue, voir index unique
    /// CardId+Language). Ne fait pas SaveChanges — à la charge de l'appelant.</summary>
    Task UpsertRangeAsync(IReadOnlyList<CardTranslation> translations, CancellationToken ct = default);

    /// <summary>
    /// Résout, pour un ensemble de cartes (par Id), leur nom dans la langue donnée.
    /// Cartes absentes du dictionnaire retourné = pas de traduction connue (repli EN côté DTO).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetNamesByCardIdsAsync(
        IReadOnlyList<Guid> cardIds, string language, CancellationToken ct = default);

    /// <summary>
    /// Recherche inversée : cartes dont le nom traduit contient <paramref name="nameQuery"/> —
    /// utilisée par le fallback vision+texte (RerunScanBatchItemsCommandHandler) pour retrouver
    /// une carte à partir d'un nom détecté par le LLM vision, quelle que soit la langue imprimée
    /// sur la carte (on ne sait pas à l'avance si c'est du FR, de l'EN ou une carte étrangère
    /// possédée par un client). <paramref name="language"/> est optionnel : null/vide = recherche
    /// sur TOUTES les langues connues de card_translations, sans filtre — c'est le mode utilisé
    /// par le fallback (corrigé le 2026-09-11 : filtrer sur "fr" en dur ratait toute carte dont le
    /// nom détecté correspondait à une traduction dans une autre langue, ou à une carte déjà en
    /// anglais mais malgré tout présente dans card_translations sous un autre code langue).
    /// Comparaison insensible à la casse (ILIKE) ; ne gère pas l'accentuation (limite connue v1 —
    /// le nom détecté par le modèle vision conserve généralement les accents imprimés sur la carte).
    /// </summary>
    Task<IReadOnlyList<Guid>> SearchCardIdsByNameAsync(
        string? language, string nameQuery, int limit, CancellationToken ct = default);
}
