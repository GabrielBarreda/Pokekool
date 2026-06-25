using MediatR;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Application.Pokemon.Queries.GetCardGrading;

/// <summary>
/// Retourne les données de gradation disponibles pour une carte.
/// Actuellement : PSA via PriceCharting (psa7/8/9/10).
/// </summary>
public sealed record GetCardGradingQuery(Guid CardId)
    : IRequest<IReadOnlyList<CardGradingDto>>;
