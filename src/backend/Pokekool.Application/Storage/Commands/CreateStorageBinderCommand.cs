using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Crée un classeur, soit dans un carton (BoxId), soit directement sur un meuble (FurnitureId + LevelIndex).
/// </summary>
public sealed record CreateStorageBinderCommand(
    string Label,
    Guid? BoxId,
    short? PositionInBox,
    Guid? FurnitureId,
    short? LevelIndex) : IRequest<StorageBinderDto>;

internal sealed class CreateStorageBinderCommandHandler
    : IRequestHandler<CreateStorageBinderCommand, StorageBinderDto>
{
    private readonly IStorageBoxRepository _boxRepo;
    private readonly IStorageFurnitureRepository _furnitureRepo;
    private readonly IStorageBinderRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateStorageBinderCommandHandler(
        IStorageBoxRepository boxRepo, IStorageFurnitureRepository furnitureRepo,
        IStorageBinderRepository repo, IUnitOfWork uow)
    {
        _boxRepo = boxRepo;
        _furnitureRepo = furnitureRepo;
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageBinderDto> Handle(CreateStorageBinderCommand request, CancellationToken ct)
    {
        StorageBinder binder;

        if (request.FurnitureId is { } furnitureId)
        {
            var furniture = await _furnitureRepo.GetByIdAsync(furnitureId, ct)
                ?? throw new DomainException("storage.furniture.not_found", "Meuble introuvable.");
            var level = request.LevelIndex ?? 0;
            if (!furniture.IsValidLevel(level))
                throw new DomainException("storage.furniture.invalid_level", "Niveau hors bornes du meuble.");
            binder = StorageBinder.CreateOnFurniture(furniture.Id, level, request.Label);
        }
        else if (request.BoxId is { } boxId)
        {
            var box = await _boxRepo.GetByIdAsync(boxId, ct)
                ?? throw new DomainException("storage.box.not_found", "Carton introuvable.");
            binder = StorageBinder.CreateInBox(box.Id, request.Label, request.PositionInBox);
        }
        else
        {
            throw new DomainException("storage.binder.no_target", "Un carton ou un meuble est requis.");
        }

        await _repo.AddAsync(binder, ct);
        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(binder);
    }
}
