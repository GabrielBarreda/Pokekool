using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.SetCardFavorite;

public sealed class SetCardFavoriteCommandHandler : IRequestHandler<SetCardFavoriteCommand, CollectionCardStatusDto>
{
    private readonly IUserCollectionRepository _collectionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public SetCardFavoriteCommandHandler(
        IUserCollectionRepository collectionRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _collectionRepo = collectionRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<CollectionCardStatusDto> Handle(SetCardFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var entry = await _collectionRepo.GetByUserAndCardAsync(userId, request.CardId, cancellationToken);

        if (entry is null)
        {
            if (!request.IsFavorite)
                return new CollectionCardStatusDto(request.CardId.ToString(), false, false, false);

            // Create not-owned favorite entry
            entry = UserCollection.CreateNotOwned(userId, request.CardId);
            entry.SetFavorite(true);
            await _collectionRepo.AddAsync(entry, cancellationToken);
        }
        else
        {
            entry.SetFavorite(request.IsFavorite);

            if (entry.CanBeDeleted())
            {
                _collectionRepo.Remove(entry);
                await _uow.SaveChangesAsync(cancellationToken);
                return new CollectionCardStatusDto(request.CardId.ToString(), false, false, false);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return new CollectionCardStatusDto(request.CardId.ToString(), entry.IsOwned, entry.IsFavorite, entry.IsPriority);
    }
}
