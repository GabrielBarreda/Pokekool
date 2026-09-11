using MediatR;
using Pokekool.Domain.Entities.Collection;

namespace Pokekool.Application.Collection.Commands.StartScanRun;

/// <summary>
/// Étape 1 du pipeline "Analyse du dossier" : ouvre un ScanRun avec sa destination, AVANT de
/// lancer l'analyse DINOv2 elle-même (voir RunScanBatchCommand, qui prend le ScanRunId en
/// retour). Popup web : "La collection appartient-elle à la boutique ?" puis, selon la réponse,
/// "Ajouter directement au stock ?" (Boutique) ou nom + motif de la collection tierce (Tiers).
/// </summary>
public sealed record StartScanRunCommand(
    ScanDestinationType DestinationType,
    bool? AddedToStockDirectly,
    string? ThirdPartyName,
    ScanReason? Reason,
    string? ReasonOtherText) : IRequest<Guid>;
