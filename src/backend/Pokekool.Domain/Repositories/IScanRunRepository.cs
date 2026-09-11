using Pokekool.Domain.Entities.Collection;

namespace Pokekool.Domain.Repositories;

public interface IScanRunRepository
{
    Task AddAsync(ScanRun scanRun, CancellationToken ct = default);

    /// <summary>Attache explicitement un ScanRunItem nouvellement créé au ChangeTracker EF Core
    /// (état Added). Nécessaire en complément de ScanRun.AddItem(...) : un ScanRunItem créé via
    /// son constructeur statique puis ajouté uniquement à la navigation Items d'un ScanRun déjà
    /// chargé/traqué n'est pas fiablement détecté comme "nouveau" par EF Core (observé : génère
    /// un UPDATE au lieu d'un INSERT → DbUpdateConcurrencyException, "0 row(s) affected").</summary>
    Task AddItemAsync(ScanRunItem item, CancellationToken ct = default);

    Task<ScanRun?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Historique des runs (sans les items — évite de charger tout le détail pour la
    /// liste), du plus récent au plus ancien.</summary>
    Task<IReadOnlyList<ScanRun>> ListSummariesAsync(CancellationToken ct = default);

    /// <summary>Détail complet d'un run (avec ses items) par son Id. Null si introuvable.</summary>
    Task<ScanRun?> GetWithItemsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Statistiques agrégées SUR TOUT L'HISTORIQUE persisté (comptages uniquement — pas de
    /// chargement d'entités) : total de photos scannées, reconnues directement (aucun refus),
    /// reconnues après correction manuelle (au moins un refus + relance), et échecs. Voir
    /// GetScanConfidenceStatsQueryHandler / widget "Indice de confiance" (auparavant limité au
    /// seul dernier run — corrigé le 2026-09-11, demande explicite de Gab).
    /// </summary>
    Task<(int TotalPhotos, int DirectMatches, int ManualMatches, int Failures)> GetConfidenceStatsAsync(
        CancellationToken ct = default);
}
