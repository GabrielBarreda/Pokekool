using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Queries.GetCollectionValue;

public sealed record GetCollectionValueQuery : IRequest<CollectionValueDto>;

public sealed class GetCollectionValueQueryHandler : IRequestHandler<GetCollectionValueQuery, CollectionValueDto>
{
    private readonly IUserCollectionRepository _collectionRepo;
    private readonly ICurrentUserService _currentUser;

    public GetCollectionValueQueryHandler(IUserCollectionRepository collectionRepo, ICurrentUserService currentUser)
    {
        _collectionRepo = collectionRepo;
        _currentUser = currentUser;
    }

    public async Task<CollectionValueDto> Handle(GetCollectionValueQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var (total, count) = await _collectionRepo.GetCollectionValueAsync(userId, cancellationToken);
        return new CollectionValueDto(total, "EUR", count);
    }
}
