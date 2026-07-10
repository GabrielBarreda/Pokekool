using MediatR;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

public sealed record GetPendingMovementsQuery : IRequest<IReadOnlyList<StorageMovementEventDto>>;

internal sealed class GetPendingMovementsQueryHandler
    : IRequestHandler<GetPendingMovementsQuery, IReadOnlyList<StorageMovementEventDto>>
{
    private readonly IStorageMovementEventRepository _repo;

    public GetPendingMovementsQueryHandler(IStorageMovementEventRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<StorageMovementEventDto>> Handle(
        GetPendingMovementsQuery request, CancellationToken ct)
    {
        var events = await _repo.ListPendingAsync(ct);
        return events.Select(StorageMapper.ToDto).ToList();
    }
}
