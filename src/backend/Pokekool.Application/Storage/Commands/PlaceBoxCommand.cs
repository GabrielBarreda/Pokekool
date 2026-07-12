using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Définit où vit un carton : sur un meuble (FurnitureId + LevelIndex) ou au sol (Position3dJson).
/// </summary>
public sealed record PlaceBoxCommand(
    Guid BoxId,
    Guid? FurnitureId,
    short? LevelIndex,
    string? Position3dJson) : IRequest<StorageBoxDto>;

internal sealed class PlaceBoxCommandHandler : IRequestHandler<PlaceBoxCommand, StorageBoxDto>
{
    private readonly IStorageBoxRepository _repo;
    private readonly IStorageFurnitureRepository _furnitureRepo;
    private readonly IUnitOfWork _uow;

    public PlaceBoxCommandHandler(
        IStorageBoxRepository repo, IStorageFurnitureRepository furnitureRepo, IUnitOfWork uow)
    {
        _repo = repo;
        _furnitureRepo = furnitureRepo;
        _uow = uow;
    }

    public async Task<StorageBoxDto> Handle(PlaceBoxCommand request, CancellationToken ct)
    {
        var box = await _repo.GetByIdAsync(request.BoxId, ct)
            ?? throw new DomainException("storage.box.not_found", "Carton introuvable.");

        if (request.FurnitureId is { } furnitureId)
        {
            var furniture = await _furnitureRepo.GetByIdAsync(furnitureId, ct)
                ?? throw new DomainException("storage.furniture.not_found", "Meuble introuvable.");
            var level = request.LevelIndex ?? 0;
            if (!furniture.IsValidLevel(level))
                throw new DomainException("storage.furniture.invalid_level", "Niveau hors bornes du meuble.");
            box.PlaceOnFurniture(furniture.Id, level);
        }
        else
        {
            box.PlaceOnFloor(request.Position3dJson);
        }

        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(box);
    }
}
