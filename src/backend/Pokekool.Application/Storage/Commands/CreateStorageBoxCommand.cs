using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

public sealed record CreateStorageBoxCommand(
    Guid RoomId,
    string LabelOrigin,
    string? Barcode,
    string? InternalCode,
    string? SupplierName,
    string? Size,
    string? Position3dJson,
    string? Notes) : IRequest<StorageBoxDto>;

internal sealed class CreateStorageBoxCommandHandler
    : IRequestHandler<CreateStorageBoxCommand, StorageBoxDto>
{
    private readonly IStorageRoomRepository _roomRepo;
    private readonly IStorageBoxRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateStorageBoxCommandHandler(
        IStorageRoomRepository roomRepo, IStorageBoxRepository repo, IUnitOfWork uow)
    {
        _roomRepo = roomRepo;
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageBoxDto> Handle(CreateStorageBoxCommand request, CancellationToken ct)
    {
        var room = await _roomRepo.GetByIdAsync(request.RoomId, ct)
            ?? throw new DomainException("storage.room.not_found", "Salle de stockage introuvable.");

        var box = StorageBox.Create(
            room.Id,
            StorageEnumMap.ParseLabelOrigin(request.LabelOrigin),
            request.Barcode, request.InternalCode, request.SupplierName,
            request.Size, request.Position3dJson, request.Notes);

        await _repo.AddAsync(box, ct);
        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(box);
    }
}
