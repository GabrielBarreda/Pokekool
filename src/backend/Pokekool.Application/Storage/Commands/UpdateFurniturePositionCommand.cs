using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>Déplace un meuble dans la vue 3D (position_3d).</summary>
public sealed record UpdateFurniturePositionCommand(
    Guid FurnitureId,
    string? Position3dJson) : IRequest<StorageFurnitureDto>;

internal sealed class UpdateFurniturePositionCommandHandler
    : IRequestHandler<UpdateFurniturePositionCommand, StorageFurnitureDto>
{
    private readonly IStorageFurnitureRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateFurniturePositionCommandHandler(IStorageFurnitureRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageFurnitureDto> Handle(UpdateFurniturePositionCommand request, CancellationToken ct)
    {
        var furniture = await _repo.GetByIdAsync(request.FurnitureId, ct)
            ?? throw new DomainException("storage.furniture.not_found", "Meuble introuvable.");
        furniture.SetPosition3d(request.Position3dJson);
        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(furniture);
    }
}
