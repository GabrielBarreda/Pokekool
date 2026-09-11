using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.AddScanRunToStock;

/// <summary>
/// Étape explicite "🏪 Ajouter au stock" du récap d'un run Boutique — appelée par le bouton du
/// modal de détail (à côté du "✕"), une fois que l'utilisateur a fini de vérifier/refuser/relancer
/// les cartes identifiées. Remplace l'ancien comportement (auto-commit pendant RunScanBatch) qui
/// committait AVANT toute correction possible, causant un dédoublement du stock si l'utilisateur
/// relançait ensuite des cartes refusées.
/// </summary>
public sealed record AddScanRunToStockCommand(Guid ScanRunId) : IRequest<AddScanRunToStockResult>;
