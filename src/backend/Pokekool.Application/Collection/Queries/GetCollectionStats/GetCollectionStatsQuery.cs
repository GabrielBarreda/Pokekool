using MediatR;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Queries.GetCollectionStats;

public sealed record GetCollectionStatsQuery : IRequest<CollectionStatsDto>;
