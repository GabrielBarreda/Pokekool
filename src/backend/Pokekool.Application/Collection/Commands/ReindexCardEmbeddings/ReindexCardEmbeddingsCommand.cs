using MediatR;

namespace Pokekool.Application.Collection.Commands.ReindexCardEmbeddings;

/// <summary>
/// Calcule et stocke l'embedding visuel (DINOv2) des cartes qui n'en ont pas encore, par lots
/// (pour rester appelable en synchrone via HTTP sans timeout). Rappeler avec le même
/// <paramref name="BatchSize"/> jusqu'à ce que Remaining == 0 pour indexer tout le catalogue.
/// Voir 03-Metier/Automatisations-LLM.md § Matching visuel (DINOv2).
/// </summary>
public sealed record ReindexCardEmbeddingsCommand(int BatchSize) : IRequest<ReindexCardEmbeddingsResult>;

public sealed record ReindexCardEmbeddingsResult(int Processed, int Failed, int Remaining);
