using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

/// <summary>Salle + meubles + cartons (avec classeurs) + classeurs posés sur meuble — payload de la vue 3D.</summary>
public sealed record GetRoomLayoutQuery(Guid RoomId) : IRequest<StorageRoomLayoutDto>;

internal sealed class GetRoomLayoutQueryHandler
    : IRequestHandler<GetRoomLayoutQuery, StorageRoomLayoutDto>
{
    private readonly IStorageRoomRepository _roomRepo;
    private readonly IStorageFurnitureRepository _furnitureRepo;
    private readonly IStorageBoxRepository _boxRepo;
    private readonly IStorageBinderRepository _binderRepo;

    public GetRoomLayoutQueryHandler(
        IStorageRoomRepository roomRepo,
        IStorageFurnitureRepository furnitureRepo,
        IStorageBoxRepository boxRepo,
        IStorageBinderRepository binderRepo)
    {
        _roomRepo = roomRepo;
        _furnitureRepo = furnitureRepo;
        _boxRepo = boxRepo;
        _binderRepo = binderRepo;
    }

    public async Task<StorageRoomLayoutDto> Handle(GetRoomLayoutQuery request, CancellationToken ct)
    {
        var room = await _roomRepo.GetByIdAsync(request.RoomId, ct)
            ?? throw new DomainException("storage.room.not_found", "Salle de stockage introuvable.");

        var furniture = await _furnitureRepo.ListByRoomAsync(room.Id, ct);
        var boxes = await _boxRepo.ListByRoomAsync(room.Id, labelOrigin: null, ct);
        var boxIds = boxes.Select(b => b.Id).ToList();

        // Classeurs contenus dans les cartons de la salle.
        var boxBinders = await _binderRepo.ListByBoxIdsAsync(boxIds, ct);
        var bindersByBox = boxBinders
            .Where(b => b.BoxId.HasValue)
            .GroupBy(b => b.BoxId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var boxDtos = boxes.Select(box =>
        {
            var list = bindersByBox.TryGetValue(box.Id, out var bs)
                ? bs.Select(StorageMapper.ToDto).ToList()
                : new List<StorageBinderDto>();
            return new StorageBoxWithBindersDto(StorageMapper.ToDto(box), list);
        }).ToList();

        // Classeurs posés directement sur un meuble (hors carton).
        var looseBinders = new List<StorageBinderDto>();
        foreach (var f in furniture)
            foreach (var b in await _binderRepo.ListByFurnitureAsync(f.Id, ct))
                looseBinders.Add(StorageMapper.ToDto(b));

        return new StorageRoomLayoutDto(
            StorageMapper.ToDto(room),
            boxDtos,
            furniture.Select(StorageMapper.ToDto).ToList(),
            looseBinders);
    }
}
