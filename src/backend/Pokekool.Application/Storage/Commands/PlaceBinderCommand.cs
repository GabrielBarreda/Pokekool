using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Définit où vit un classeur : dans un carton (BoxId) ou sur un meuble (FurnitureId + LevelIndex).
/// </summary>
public sealed record PlaceBinderCommand(
    Guid BinderId,
    Guid? BoxId,
    short? PositionInBox,
    Guid? FurnitureId,
    short? LevelIndex) : IRequest<StorageBinderDto>;

internal sealed class PlaceBinderCommandHandler : IRequestHandler<PlaceBinderCommand, StorageBinderDto>
{
    private readonly IStorageBinderRepository _repo;
    private readonly IStorageBoxRepository _boxRepo;
    private readonly IStorageFurnitureRepository _furnitureRepo;
    private readonly IUnitOfWork _uow;

    public PlaceBinderCommandHandler(
        IStorageBinderRepository repo, IStorageBoxRepository boxRepo,
        IStorageFurnitureRepository furnitureRepo, IUnitOfWork uow)
    {
        _repo = repo;
        _boxRepo = boxRepo;
        _furnitureRepo = furnitureRepo;
        _uow = uow;
    }

    public async Task<StorageBinderDto> Handle(PlaceBinderCommand request, CancellationToken ct)
    {
        var binder = await _repo.GetByIdAsync(request.BinderId, ct)
            ?? throw new DomainException("storage.binder.not_found", "Classeur introuvable.");

        if (request.FurnitureId is { } furnitureId)
        {
            var furniture = await _furnitureRepo.GetByIdAsync(furnitureId, ct)
                ?? throw new DomainException("storage.furniture.not_found", "Meuble introuvable.");
            var level = request.LevelIndex ?? 0;
            if (!furniture.IsValidLevel(level))
                throw new DomainException("storage.furniture.invalid_level", "Niveau hors bornes du meuble.");
            binder.PlaceOnFurniture(furniture.Id, level);
        }
        else if (request.BoxId is { } boxId)
        {
            _ = await _boxRepo.GetByIdAsync(boxId, ct)
                ?? throw new DomainException("storage.box.not_found", "Carton introuvable.");
            binder.PlaceInBox(boxId, request.PositionInBox);
        }
        else
        {
            throw new DomainException("storage.binder.no_target", "Un carton ou un meuble est requis.");
        }

        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(binder);
    }
}
