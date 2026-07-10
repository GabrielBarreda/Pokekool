using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

/// <summary>Statut d'un job de compilation Blender (polling front toutes les ~3s).</summary>
public sealed record GetCompileJobStatusQuery(Guid JobId) : IRequest<StorageCompileJobDto>;

internal sealed class GetCompileJobStatusQueryHandler
    : IRequestHandler<GetCompileJobStatusQuery, StorageCompileJobDto>
{
    private readonly IStorageModelCompileJobRepository _repo;

    public GetCompileJobStatusQueryHandler(IStorageModelCompileJobRepository repo) => _repo = repo;

    public async Task<StorageCompileJobDto> Handle(GetCompileJobStatusQuery request, CancellationToken ct)
    {
        var job = await _repo.GetByIdAsync(request.JobId, ct)
            ?? throw new DomainException("storage.compile.job_not_found", "Job de compilation introuvable.");
        return StorageMapper.ToDto(job);
    }
}
