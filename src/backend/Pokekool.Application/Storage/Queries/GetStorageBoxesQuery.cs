using MediatR;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

public sealed record GetStorageBoxesQuery(
    Guid RoomId,
    string? LabelOrigin = null) : IRequest<IReadOnlyList<StorageBoxDto>>;

internal sealed class GetStorageBoxesQueryHandler
    : IRequestHandler<GetStorageBoxesQuery, IReadOnlyList<StorageBoxDto>>
{
    private readonly IStorageBoxRepository _repo;

    public GetStorageBoxesQueryHandler(IStorageBoxRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<StorageBoxDto>> Handle(GetStorageBoxesQuery request, CancellationToken ct)
    {
        var origin = StorageEnumMap.ParseLabelOriginOrNull(request.LabelOrigin);
        var boxes = await _repo.ListByRoomAsync(request.RoomId, origin, ct);
        return boxes.Select(StorageMapper.ToDto).ToList();
    }
}
