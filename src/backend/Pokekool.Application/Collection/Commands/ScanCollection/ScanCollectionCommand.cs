using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.ScanCollection;

/// <param name="ImageBase64">Image encodée à analyser.</param>
/// <param name="Exclusions">
/// Libellés de cartes déjà rejetés par l'utilisateur (re-roll) : injectés en contrainte
/// négative dans le prompt pour que le modèle ne les repropose pas.
/// Conservé pour le workflow texte (désactivé, voir ScanCollectionCommandHandler) — inerte
/// tant que le matching visuel pur est actif.
/// </param>
/// <param name="ExcludedCardIds">
/// Comparatif DINOv2 (2026-09-08) : cartes déjà refusées par l'utilisateur sur cette même
/// photo — exclues du prochain top-5 visuel (SearchByEmbeddingAsync) pour que le re-roll
/// propose bien 5 nouveaux candidats plutôt que de reproposer un candidat déjà rejeté.
/// </param>
public sealed record ScanCollectionCommand(
    string ImageBase64,
    IReadOnlyList<string>? Exclusions = null,
    IReadOnlyList<Guid>? ExcludedCardIds = null) : IRequest<ScanCollectionResult>;
