using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Assigne en lot plusieurs items du stock boutique à un classeur (un appel plutôt que N).
/// </summary>
public sealed record BatchAssignStorageLocationCommand(
    Guid BinderId,
    string OwnerType,
    IReadOnlyList<Guid> OwnerIds) : IRequest<IReadOnlyList<StorageLocationDto>>;

internal sealed class BatchAssignStorageLocationCommandHandler
    : IRequestHandler<BatchAssignStorageLocationCommand, IReadOnlyList<StorageLocationDto>>
{
    private readonly IStorageBinderRepository _binderRepo;
    private readonly IStorageLocationRepository _repo;
    private readonly IUnitOfWork _uow;

    public BatchAssignStorageLocationCommandHandler(
        IStorageBinderRepository binderRepo, IStorageLocationRepository repo, IUnitOfWork uow)
    {
        _binderRepo = binderRepo;
        _repo = repo;
        _uow = uow;
    }

    public async Task<IReadOnlyList<StorageLocationDto>> Handle(
        BatchAssignStorageLocationCommand request, CancellationToken ct)
    {
        _ = await _binderRepo.GetByIdAsync(request.BinderId, ct)
            ?? throw new DomainException("storage.binder.not_found", "Classeur introuvable.");

        var results = new List<StorageLocationDto>(request.OwnerIds.Count);
        foreach (var ownerId in request.OwnerIds)
        {
            // Déplace la localisation existante plutôt que d'en créer une seconde (anti-doublon).
            var existing = await _repo.FindByOwnerAsync(request.OwnerType, ownerId, ct);
            StorageLocation location;
            if (existing is not null)
            {
                existing.MoveTo(request.BinderId, null);
                location = existing;
            }
            else
            {
                location = StorageLocation.Create(request.BinderId, request.OwnerType, ownerId, null);
                await _repo.AddAsync(location, ct);
            }
            results.Add(StorageMapper.ToDto(location));
        }

        await _uow.SaveChangesAsync(ct);
        return results;
    }
}
