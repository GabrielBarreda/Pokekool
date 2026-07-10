using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

/// <summary>Salle + tous ses cartons (avec classeurs) — payload de chargement de la vue 3D.</summary>
public sealed record GetRoomLayoutQuery(Guid RoomId) : IRequest<StorageRoomLayoutDto>;

internal sealed class GetRoomLayoutQueryHandler
    : IRequestHandler<GetRoomLayoutQuery, StorageRoomLayoutDto>
{
    private readonly IStorageRoomRepository _roomRepo;
    private readonly IStorageBoxRepository _boxRepo;
    private readonly IStorageBinderRepository _binderRepo;

    public GetRoomLayoutQueryHandler(
        IStorageRoomRepository roomRepo,
        IStorageBoxRepository boxRepo,
        IStorageBinderRepository binderRepo)
    {
        _roomRepo = roomRepo;
        _boxRepo = boxRepo;
        _binderRepo = binderRepo;
    }

    public async Task<StorageRoomLayoutDto> Handle(GetRoomLayoutQuery request, CancellationToken ct)
    {
        var room = await _roomRepo.GetByIdAsync(request.RoomId, ct)
            ?? throw new DomainException("storage.room.not_found", "Salle de stockage introuvable.");

        var boxes = await _boxRepo.ListByRoomAsync(room.Id, labelOrigin: null, ct);
        var boxIds = boxes.Select(b => b.Id).ToList();
        var binders = await _binderRepo.ListByBoxIdsAsync(boxIds, ct);
        var bindersByBox = binders.GroupBy(b => b.BoxId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var boxDtos = boxes.Select(box =>
        {
            var boxBinders = bindersByBox.TryGetValue(box.Id, out var list)
                ? list.Select(StorageMapper.ToDto).ToList()
                : new List<StorageBinderDto>();
            return new StorageBoxWithBindersDto(StorageMapper.ToDto(box), boxBinders);
        }).ToList();

        return new StorageRoomLayoutDto(StorageMapper.ToDto(room), boxDtos);
    }
}
