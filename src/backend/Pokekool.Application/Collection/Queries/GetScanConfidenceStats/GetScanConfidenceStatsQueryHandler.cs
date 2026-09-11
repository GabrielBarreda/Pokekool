using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Queries.GetScanConfidenceStats;

/// <summary>
/// Calcule l'indice de confiance sur TOUT l'historique persisté des scans (et non plus seulement
/// le dernier run — correction du 2026-09-11, demande explicite de Gab : "cet indice a pas bougé
/// depuis tout à l'heure, il n'est basé que sur le dernier scan, hors on a désormais un historique
/// persisté, il doit prendre en compte tout l'historique (les erreurs sont les cartes pas reconnues
/// et qui ont mené à une association manuelle)"). Voir ScanConfidenceStatsDto pour la définition
/// exacte des trois catégories (DirectMatches / ManualMatches / Failures).
/// </summary>
public sealed class GetScanConfidenceStatsQueryHandler
    : IRequestHandler<GetScanConfidenceStatsQuery, ScanConfidenceStatsDto>
{
    private readonly IScanRunRepository _scanRunRepo;

    public GetScanConfidenceStatsQueryHandler(IScanRunRepository scanRunRepo)
    {
        _scanRunRepo = scanRunRepo;
    }

    public async Task<ScanConfidenceStatsDto> Handle(
        GetScanConfidenceStatsQuery request, CancellationToken cancellationToken)
    {
        var (totalPhotos, directMatches, manualMatches, failures) =
            await _scanRunRepo.GetConfidenceStatsAsync(cancellationToken);

        double? confidencePercent = totalPhotos > 0
            ? Math.Round((double)directMatches / totalPhotos * 100, 1)
            : null;

        return new ScanConfidenceStatsDto(
            totalPhotos,
            directMatches,
            manualMatches,
            failures,
            confidencePercent);
    }
}
