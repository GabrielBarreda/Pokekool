using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Supprime un meuble. Les cartons/classeurs qu'il contenait sont <b>détachés</b> (FK SET NULL en base),
/// pas supprimés — on ne perd pas de contenu. Ils redeviennent « non rangés ».
/// </summary>
public sealed record DeleteStorageFurnitureCommand(Guid FurnitureId) : IRequest;

internal sealed class DeleteStorageFurnitureCommandHandler
    : IRequestHandler<DeleteStorageFurnitureCommand>
{
    private readonly IStorageFurnitureRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteStorageFurnitureCommandHandler(IStorageFurnitureRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DeleteStorageFurnitureCommand request, CancellationToken ct)
    {
        var furniture = await _repo.GetByIdAsync(request.FurnitureId, ct)
            ?? throw new DomainException("storage.furniture.not_found", "Meuble introuvable.");

        // La contrainte FK (ON DELETE SET NULL) détache automatiquement cartons & classeurs.
        _repo.Remove(furniture);
        await _uow.SaveChangesAsync(ct);
    }
}
