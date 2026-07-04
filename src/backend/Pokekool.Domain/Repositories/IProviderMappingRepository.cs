namespace Pokekool.Domain.Repositories;

/// <summary>
/// Repository pour les mappings provider ↔ entités (sets et cartes).
/// Remplace les colonnes pricecharting_slug, cm_expansion_id, etc. sur les entités.
/// </summary>
public interface IProviderMappingRepository
{
    // ── Sets ─────────────────────────────────────────────────────────────────

    /// <summary>Retourne la valeur d'un mapping set, ou null si absent.</summary>
    Task<string?> GetSetValueAsync(
        Guid setId, string provider, string key, CancellationToken ct = default);

    /// <summary>Retourne tous les mappings d'un set pour un provider donné.</summary>
    Task<IReadOnlyDictionary<string, string>> GetSetMappingsAsync(
        Guid setId, string provider, CancellationToken ct = default);

    /// <summary>
    /// Insère ou met à jour le mapping set.
    /// Attention : nécessite SaveChanges via IUnitOfWork.
    /// </summary>
    Task UpsertSetMappingAsync(
        Guid setId, string provider, string key, string value, CancellationToken ct = default);

    // ── Cartes ───────────────────────────────────────────────────────────────

    /// <summary>Retourne la valeur d'un mapping carte, ou null si absent.</summary>
    Task<string?> GetCardValueAsync(
        Guid cardId, string provider, string key, CancellationToken ct = default);

    /// <summary>
    /// Retourne un dictionnaire cardId → value pour toutes les cartes d'un set
    /// ayant un mapping pour (provider, key). Utilisé pour les batch lookups.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetCardValuesBySetAsync(
        Guid setId, string provider, string key, CancellationToken ct = default);

    /// <summary>
    /// Retourne un dictionnaire cardId → value pour une liste de tcgIds.
    /// Plus efficace que N appels GetCardValueAsync pour les providers qui traitent par batch.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetCardValuesByCardIdsAsync(
        IEnumerable<Guid> cardIds, string provider, string key, CancellationToken ct = default);

    /// <summary>
    /// Insère ou met à jour le mapping carte.
    /// Attention : nécessite SaveChanges via IUnitOfWork.
    /// </summary>
    Task UpsertCardMappingAsync(
        Guid cardId, string provider, string key, string value, CancellationToken ct = default);
}
