using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.SetCardPriority;

public sealed class SetCardPriorityCommandHandler : IRequestHandler<SetCardPriorityCommand, CollectionCardStatusDto>
{
    private readonly IUserCollectionRepository _collectionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public SetCardPriorityCommandHandler(
        IUserCollectionRepository collectionRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _collectionRepo = collectionRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<CollectionCardStatusDto> Handle(SetCardPriorityCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var entry = await _collectionRepo.GetByUserAndCardAsync(userId, request.CardId, cancellationToken);

        if (request.IsPriority)
        {
            if (entry is not null && entry.IsOwned)
                throw new Pokekool.Domain.Common.DomainException(
                    "collection.priority_on_owned",
                    "Cannot mark an owned card as priority.");

            if (entry is null)
            {
                entry = UserCollection.CreateNotOwned(userId, request.CardId);
                entry.SetPriority(true);
                await _collectionRepo.AddAsync(entry, cancellationToken);
            }
            else
            {
                entry.SetPriority(true);
            }
        }
        else
        {
            if (entry is null)
                return new CollectionCardStatusDto(request.CardId.ToString(), false, false, false);

            entry.SetPriority(false);

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
