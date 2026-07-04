namespace Pokekool.Application.DataCollection;

/// <summary>
/// Sémaphore scoped (une instance par requête HTTP) partagé entre tous les
/// ICardPriceProvider et DataAggregator pour sérialiser les accès EF Core.
///
/// Problème : quand DataAggregator lance plusieurs providers en parallèle
/// (Task.WhenAll), chacun a son propre SemaphoreSlim(1,1) interne — mais ils
/// partagent tous le même DbContext scoped. Deux providers peuvent donc lancer
/// deux opérations DB simultanément → "A second operation was started on this
/// context instance before a previous operation completed."
///
/// Solution : un seul sémaphore scoped injecté dans tous les providers +
/// DataAggregator. Les opérations HTTP (lentes) restent entièrement parallèles ;
/// seules les opérations DB (rapides, ~2–5ms) sont sérialisées.
/// </summary>
public sealed class DbConcurrencyGuard
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}
