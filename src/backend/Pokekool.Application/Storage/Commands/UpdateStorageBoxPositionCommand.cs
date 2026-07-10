using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>Met à jour la position 3D d'un carton (déplacement dans le viewer three.js).</summary>
public sealed record UpdateStorageBoxPositionCommand(
    Guid BoxId,
    string? Position3dJson) : IRequest<StorageBoxDto>;

internal sealed class UpdateStorageBoxPositionCommandHandler
    : IRequestHandler<UpdateStorageBoxPositionCommand, StorageBoxDto>
{
    private readonly IStorageBoxRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateStorageBoxPositionCommandHandler(IStorageBoxRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageBoxDto> Handle(UpdateStorageBoxPositionCommand request, CancellationToken ct)
    {
        var box = await _repo.GetByIdAsync(request.BoxId, ct)
            ?? throw new DomainException("storage.box.not_found", "Carton introuvable.");

        box.SetPosition3d(request.Position3dJson);
        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(box);
    }
}
