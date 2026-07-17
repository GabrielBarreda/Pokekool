using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>Rattache un item existant (ShopStock / collection utilisateur) à un classeur.</summary>
public sealed record AssignStorageLocationCommand(
    Guid BinderId,
    string OwnerType,
    Guid OwnerId,
    short? SlotPosition) : IRequest<StorageLocationDto>;

internal sealed class AssignStorageLocationCommandHandler
    : IRequestHandler<AssignStorageLocationCommand, StorageLocationDto>
{
    private readonly IStorageBinderRepository _binderRepo;
    private readonly IStorageLocationRepository _repo;
    private readonly IUnitOfWork _uow;

    public AssignStorageLocationCommandHandler(
        IStorageBinderRepository binderRepo, IStorageLocationRepository repo, IUnitOfWork uow)
    {
        _binderRepo = binderRepo;
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageLocationDto> Handle(AssignStorageLocationCommand request, CancellationToken ct)
    {
        _ = await _binderRepo.GetByIdAsync(request.BinderId, ct)
            ?? throw new DomainException("storage.binder.not_found", "Classeur introuvable.");

        // Une carte est rangée dans au plus un classeur : si elle l'est déjà, on la DÉPLACE
        // (mise à jour) au lieu de créer un doublon.
        var existing = await _repo.FindByOwnerAsync(request.OwnerType, request.OwnerId, ct);
        StorageLocation location;
        if (existing is not null)
        {
            existing.MoveTo(request.BinderId, request.SlotPosition);
            location = existing;
        }
        else
        {
            location = StorageLocation.Create(
                request.BinderId, request.OwnerType, request.OwnerId, request.SlotPosition);
            await _repo.AddAsync(location, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(location);
    }
}
