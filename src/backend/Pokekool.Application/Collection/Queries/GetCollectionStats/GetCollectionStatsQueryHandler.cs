using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Queries.GetCollectionStats;

public sealed class GetCollectionStatsQueryHandler : IRequestHandler<GetCollectionStatsQuery, CollectionStatsDto>
{
    private readonly IUserCollectionRepository _collectionRepo;
    private readonly ICurrentUserService _currentUser;

    public GetCollectionStatsQueryHandler(
        IUserCollectionRepository collectionRepo,
        ICurrentUserService currentUser)
    {
        _collectionRepo = collectionRepo;
        _currentUser = currentUser;
    }

    public async Task<CollectionStatsDto> Handle(GetCollectionStatsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var stats = await _collectionRepo.GetStatsAsync(userId, cancellationToken);

        var completionPercent = stats.TotalCards > 0
            ? Math.Round((double)stats.OwnedCards / stats.TotalCards * 100, 2)
            : 0;

        return new CollectionStatsDto(
            stats.TotalCards,
            stats.OwnedCards,
            stats.FavoriteCards,
            stats.PriorityCards,
            completionPercent);
    }
}
