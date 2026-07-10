using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.BulkAddToCollection;

public sealed class BulkAddToCollectionCommandHandler
    : IRequestHandler<BulkAddToCollectionCommand, BulkAddResult>
{
    private readonly IUserCollectionRepository _collectionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IVaultKnowledgeService _vault;

    public BulkAddToCollectionCommandHandler(
        IUserCollectionRepository collectionRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IVaultKnowledgeService vault)
    {
        _collectionRepo = collectionRepo;
        _currentUser = currentUser;
        _uow = uow;
        _vault = vault;
    }

    public async Task<BulkAddResult> Handle(
        BulkAddToCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var addedIds = new List<Guid>();
        var alreadyOwned = 0;

        foreach (var cardId in request.CardIds.Distinct())
        {
            var entry = await _collectionRepo.GetByUserAndCardAsync(userId, cardId, cancellationToken);

            if (entry is null)
            {
                entry = UserCollection.Create(userId, cardId);
                await _collectionRepo.AddAsync(entry, cancellationToken);
                addedIds.Add(cardId);
            }
            else if (entry.IsOwned)
            {
                alreadyOwned++;
            }
            else
            {
                // Ligne existante (favori / prioritaire) non possédée → on la marque possédée.
                entry.ToggleOwned();
                addedIds.Add(cardId);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Enrichissement du Vault pour chaque carte réellement ajoutée.
        foreach (var cardId in addedIds)
        {
            var dto = request.Matches.FirstOrDefault(m => m.CardId == cardId);
            if (dto is not null)
                await _vault.UpdateCardEntryAsync(dto, dto.Confidence, cancellationToken);
        }

        await _vault.UpdateStatsAsync(request.Matches.Count, addedIds.Count, cancellationToken);

        return new BulkAddResult(addedIds.Count, alreadyOwned);
    }
}
