using MediatR;

namespace Pokekool.Application.Pokemon.Commands.SyncCardPrices;

/// <summary>
/// Synchronise les prix des cartes via tous les ICardPriceProvider enregistrés.
/// </summary>
/// <param name="CardIds">
///   IDs explicites à synchroniser. Si null/vide, on sélectionne toutes les cartes
///   périmées selon <paramref name="StalenessHours"/>.
/// </param>
/// <param name="StalenessHours">
///   Durée de fraîcheur en heures (défaut 24h).
///   Une carte avec un prix de moins de X heures est skippée.
///   Mettre à 0 pour forcer le re-sync de toutes les cartes.
/// </param>
public sealed record SyncCardPricesCommand(
    IEnumerable<Guid>? CardIds       = null,
    int                StalenessHours = 24)
    : IRequest<SyncPricesResultDto>;

public sealed record SyncPricesResultDto(
    int PricesUpserted,
    int CardsFailed,
    IReadOnlyList<string>? Errors);
