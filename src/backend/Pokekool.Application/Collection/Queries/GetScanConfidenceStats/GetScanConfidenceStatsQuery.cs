using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Queries.GetScanConfidenceStats;

/// <summary>Indice de confiance agrégé sur tout l'historique persisté des scans — voir
/// ScanConfidenceStatsDto / GetScanConfidenceStatsQueryHandler.</summary>
public sealed record GetScanConfidenceStatsQuery : IRequest<ScanConfidenceStatsDto>;
