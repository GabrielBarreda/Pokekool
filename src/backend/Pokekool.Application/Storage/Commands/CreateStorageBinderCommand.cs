using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

public sealed record CreateStorageBinderCommand(
    Guid BoxId,
    string Label,
    short? PositionInBox) : IRequest<StorageBinderDto>;

internal sealed class CreateStorageBinderCommandHandler
    : IRequestHandler<CreateStorageBinderCommand, StorageBinderDto>
{
    private readonly IStorageBoxRepository _boxRepo;
    private readonly IStorageBinderRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateStorageBinderCommandHandler(
        IStorageBoxRepository boxRepo, IStorageBinderRepository repo, IUnitOfWork uow)
    {
        _boxRepo = boxRepo;
        _repo = repo;
        _uow = uow;
    }

    public async Task<StorageBinderDto> Handle(CreateStorageBinderCommand request, CancellationToken ct)
    {
        var box = await _boxRepo.GetByIdAsync(request.BoxId, ct)
            ?? throw new DomainException("storage.box.not_found", "Carton introuvable.");

        var binder = StorageBinder.Create(box.Id, request.Label, request.PositionInBox);
        await _repo.AddAsync(binder, ct);
        await _uow.SaveChangesAsync(ct);
        return StorageMapper.ToDto(binder);
    }
}
