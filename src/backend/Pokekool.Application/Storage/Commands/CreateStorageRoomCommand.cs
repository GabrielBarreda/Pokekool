using MediatR;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

public sealed record CreateStorageRoomCommand(string Name) : IRequest<StorageRoomDto>;

internal sealed class CreateStorageRoomCommandHandler
    : IRequestHandler<CreateStorageRoomCommand, StorageRoomDto>
{
    private readonly IShopRepository _shopRepo;
    private readonly IStorageRoomRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateStorageRoomCommandHandler(
        IShopRepository shopRepo, IStorageRoomRepository repo, IUnitOfWork uow)
    {
        _shopRepo = shopRepo;
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageRoomDto> Handle(CreateStorageRoomCommand request, CancellationToken ct)
    {
        var shop = await _shopRepo.GetOrCreateAsync(ct);
        var room = StorageRoom.Create(shop.Id, request.Name);
        await _repo.AddAsync(room, ct);
        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(room);
    }
}
