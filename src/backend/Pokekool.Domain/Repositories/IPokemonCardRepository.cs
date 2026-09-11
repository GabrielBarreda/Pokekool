using Pokekool.Domain.Entities.Pokemon;

namespace Pokekool.Domain.Repositories;

public interface IPokemonCardRepository
{
    Task<PokemonCard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PokemonCard?> GetByTcgIdAsync(string tcgId, CancellationToken ct = default);
    Task<(IReadOnlyList<PokemonCard> Items, int TotalCount)> SearchAsync(
        string? search,
        Guid? setId,
        string? rarity,
        string? supertype,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Recherche sur le nom canonique (EN) insensible à la casse (ILIKE) — utilisée par le
    /// fallback texte/vision du scan batch Admin (RerunScanBatchItemsCommandHandler), où le nom
    /// lu par le modèle vision peut différer en casse du nom stocké en base. Contrairement à
    /// <see cref="SearchAsync"/> (recherche générale de l'UI admin, `Contains` — sensible à la
    /// casse sous Postgres), celle-ci ne doit jamais rater un match à cause de la casse. Bug réel
    /// observé le 2026-09-11 : "Skitty" (lu par le LLM vision, identique au nom EN) non trouvé
    /// car le seed passé à SearchAsync était pré-normalisé en minuscules par Normalize().
    /// </summary>
    Task<IReadOnlyList<PokemonCard>> SearchByNameIgnoreCaseAsync(
        string nameQuery, int limit, CancellationToken ct = default);
    Task AddAsync(PokemonCard card, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PokemonCard> cards, CancellationToken ct = default);
    Task<IReadOnlyList<PokemonCard>> GetAllAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetIdsBySetIdAsync(Guid setId, CancellationToken ct = default);

    /// <summary>
    /// Id interne + TcgId de toutes les cartes d'un set — utilisé par le sync des traductions
    /// pour matcher les cartes locales contre la réponse TCGdex (identifiée par TcgId) sans
    /// charger les entités complètes (voir SyncCardTranslationsCommandHandler).
    /// </summary>
    Task<IReadOnlyList<(Guid Id, string TcgId)>> GetIdAndTcgIdBySetIdAsync(Guid setId, CancellationToken ct = default);

    /// <summary>
    /// Rarités et supertypes distincts présents dans un set.
    /// Utilisé pour griser les filtres indisponibles dans la bourse.
    /// </summary>
    Task<(IReadOnlyList<string> Rarities, IReadOnlyList<string> Supertypes)> GetDistinctFiltersAsync(
        Guid setId, CancellationToken ct = default);

    // ── Matching visuel (DINOv2) — comparatif 2026-09-04, voir Automatisations-LLM.md ──────
    // Exposé en termes de float[] uniquement (jamais Pgvector.Vector) pour que le Domain reste
    // libre de toute dépendance ORM/PostgreSQL — le type Vector est confiné à Infrastructure
    // (shadow property EF, voir PokemonCardConfiguration + PokemonCardRepository).

    /// <summary>Stocke/remplace l'embedding visuel d'une carte. Ne fait pas SaveChanges — à la
    /// charge de l'appelant (cohérent avec AddAsync/AddRangeAsync ci-dessus).</summary>
    Task SetEmbeddingAsync(Guid cardId, float[] embedding, CancellationToken ct = default);

    /// <summary>Cartes sans embedding indexé — utilisé par le job d'indexation initiale/incrémentale.</summary>
    Task<IReadOnlyList<PokemonCard>> GetWithoutEmbeddingAsync(int batchSize, CancellationToken ct = default);

    /// <summary>Nombre de cartes sans embedding indexé — pour savoir quand le job d'indexation a fini.</summary>
    Task<int> CountWithoutEmbeddingAsync(CancellationToken ct = default);

    /// <summary>
    /// Recherche par similarité visuelle (distance cosine sur l'embedding DINOv2) — les
    /// <paramref name="topN"/> cartes les plus proches du vecteur donné, triées par distance
    /// croissante (0 = identique). Ignore les cartes sans embedding indexé.
    /// </summary>
    /// <param name="excludedCardIds">
    /// Cartes à ignorer (ré-itération après refus utilisateur — workflow scan visuel pur,
    /// voir ScanCollectionCommandHandler). Null/vide = aucune exclusion.
    /// </param>
    Task<IReadOnlyList<(PokemonCard Card, double Distance)>> SearchByEmbeddingAsync(
        float[] embedding, int topN, IReadOnlyList<Guid>? excludedCardIds = null, CancellationToken ct = default);

    /// <summary>
    /// Variante restreinte de <see cref="SearchByEmbeddingAsync"/> — distance cosine calculée
    /// UNIQUEMENT parmi <paramref name="candidateCardIds"/> (au lieu de tout le catalogue).
    /// Utilisée par le fallback texte/vision (RerunScanBatchItemsCommandHandler) : quand llava/OCR
    /// lit un nom qui matche plusieurs cartes en base (ex. "Skitty" → ~20 versions/sets), on
    /// laisse DINOv2 arbitrer entre CES candidats précis plutôt que de renoncer (candidats > 1) ou
    /// de rechercher sur tout le catalogue (inutile, on sait déjà que le bon candidat est dans ce
    /// sous-ensemble). Beaucoup plus léger qu'un appel llava supplémentaire — combine la force de
    /// chaque modèle (llava lit le nom, DINOv2 discrimine visuellement les variantes du même nom)
    /// sans le coût CPU d'un second passage vision. Retourne une liste vide si
    /// <paramref name="candidateCardIds"/> est vide.
    /// </summary>
    Task<IReadOnlyList<(PokemonCard Card, double Distance)>> SearchByEmbeddingAmongAsync(
        float[] embedding, IReadOnlyList<Guid> candidateCardIds, int topN, CancellationToken ct = default);
}
