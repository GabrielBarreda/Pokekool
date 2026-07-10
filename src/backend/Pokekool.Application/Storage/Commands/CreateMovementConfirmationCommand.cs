using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Un opérateur confirme (ou rejette) un événement de déplacement détecté.
/// La détection caméra elle-même n'est pas dans ce prompt.
/// </summary>
public sealed record CreateMovementConfirmationCommand(
    Guid EventId,
    bool Confirm) : IRequest<StorageMovementEventDto>;

internal sealed class CreateMovementConfirmationCommandHandler
    : IRequestHandler<CreateMovementConfirmationCommand, StorageMovementEventDto>
{
    private readonly IStorageMovementEventRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateMovementConfirmationCommandHandler(
        IStorageMovementEventRepository repo, ICurrentUserService currentUser, IUnitOfWork uow)
    {
        _repo = repo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<StorageMovementEventDto> Handle(
        CreateMovementConfirmationCommand request, CancellationToken ct)
    {
        var evt = await _repo.GetByIdAsync(request.EventId, ct)
            ?? throw new DomainException("storage.movement.not_found", "Événement de déplacement introuvable.");

        var userId = _currentUser.UserId
            ?? throw new DomainException("storage.movement.no_user", "Utilisateur non authentifié.");

        if (request.Confirm) evt.Confirm(userId);
        else evt.Reject(userId);

        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(evt);
    }
}
