using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

public sealed record CreateStorageFurnitureCommand(
    Guid RoomId,
    string Type,
    string? Name) : IRequest<StorageFurnitureDto>;

internal sealed class CreateStorageFurnitureCommandHandler
    : IRequestHandler<CreateStorageFurnitureCommand, StorageFurnitureDto>
{
    private readonly IStorageRoomRepository _roomRepo;
    private readonly IStorageFurnitureRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateStorageFurnitureCommandHandler(
        IStorageRoomRepository roomRepo, IStorageFurnitureRepository repo, IUnitOfWork uow)
    {
        _roomRepo = roomRepo;
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageFurnitureDto> Handle(CreateStorageFurnitureCommand request, CancellationToken ct)
    {
        _ = await _roomRepo.GetByIdAsync(request.RoomId, ct)
            ?? throw new DomainException("storage.room.not_found", "Salle de stockage introuvable.");

        var furniture = StorageFurniture.Create(
            request.RoomId, StorageEnumMap.ParseFurnitureType(request.Type), request.Name);

        await _repo.AddAsync(furniture, ct);
        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(furniture);
    }
}
