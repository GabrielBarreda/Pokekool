using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.RunScanBatch;

/// <summary>
/// Analyse en DINOv2 pur toutes les photos du dossier scan-batch/_in, écrit un rapport CSV dans
/// scan-batch/_out puis y déplace les photos traitées. Rattaché à un ScanRun déjà ouvert (voir
/// StartScanRunCommand — popup de destination/motif) : ScanRunId identifie le run à compléter
/// avec les stats + items persistés (remplace l'ancien historique fichier local).
/// </summary>
public sealed record RunScanBatchCommand(Guid ScanRunId) : IRequest<ScanBatchResult>;
