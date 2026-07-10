using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

public sealed record GetStorageRoomsQuery : IRequest<IReadOnlyList<StorageRoomDto>>;

internal sealed class GetStorageRoomsQueryHandler
    : IRequestHandler<GetStorageRoomsQuery, IReadOnlyList<StorageRoomDto>>
{
    private readonly IShopRepository _shopRepo;
    private readonly IStorageRoomRepository _repo;

    public GetStorageRoomsQueryHandler(IShopRepository shopRepo, IStorageRoomRepository repo)
    {
        _shopRepo = shopRepo;
        _repo = repo;
    }

    public async Task<IReadOnlyList<StorageRoomDto>> Handle(GetStorageRoomsQuery request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);
        var rooms = await _repo.ListByShopAsync(shop.Id, ct);
        return rooms.Select(StorageMapper.ToDto).ToList();
    }
}
