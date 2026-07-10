using MediatR;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

public sealed record GetStorageBindersQuery(Guid BoxId) : IRequest<IReadOnlyList<StorageBinderDto>>;

internal sealed class GetStorageBindersQueryHandler
    : IRequestHandler<GetStorageBindersQuery, IReadOnlyList<StorageBinderDto>>
{
    private readonly IStorageBinderRepository _repo;

    public GetStorageBindersQueryHandler(IStorageBinderRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<StorageBinderDto>> Handle(GetStorageBindersQuery request, CancellationToken ct)
    {
        var binders = await _repo.ListByBoxAsync(request.BoxId, ct);
        return binders.Select(StorageMapper.ToDto).ToList();
    }
}
