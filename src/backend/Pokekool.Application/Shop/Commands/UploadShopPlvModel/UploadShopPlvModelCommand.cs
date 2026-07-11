using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Storage;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Shop.Commands.UploadShopPlvModel;

/// <summary>
/// L'admin uploade un projet Blender (.blend) de la PLV (présentoir) de la boutique.
/// On persiste le fichier, on crée un job de compilation (status=queued, cible <c>shop</c>) et
/// on l'enfile. La compilation .blend → .glb est asynchrone (cf. <c>BlenderCompileBackgroundService</c>)
/// ; le front poll le statut. Réutilise exactement les abstractions du module Stockage
/// (IBlenderFileStore, IStorageModelCompileQueue, StorageModelCompileJob) — sans les dupliquer.
/// </summary>
public sealed record UploadShopPlvModelCommand(
    Stream BlendFileStream,
    string FileName) : IRequest<StorageCompileJobDto>;

internal sealed class UploadShopPlvModelCommandHandler
    : IRequestHandler<UploadShopPlvModelCommand, StorageCompileJobDto>
{
    private readonly IShopRepository _shopRepo;
    private readonly IStorageModelCompileJobRepository _jobRepo;
    private readonly IBlenderFileStore _fileStore;
    private readonly IStorageModelCompileQueue _queue;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public UploadShopPlvModelCommandHandler(
        IShopRepository shopRepo,
        IStorageModelCompileJobRepository jobRepo,
        IBlenderFileStore fileStore,
        IStorageModelCompileQueue queue,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _shopRepo = shopRepo;
        _jobRepo = jobRepo;
        _fileStore = fileStore;
        _queue = queue;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<StorageCompileJobDto> Handle(UploadShopPlvModelCommand request, CancellationToken ct)
    {
        // Boutique = singleton, créée vide au premier accès.
        var shop = await _shopRepo.GetOrCreateAsync(ct);

        if (!request.FileName.EndsWith(".blend", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("shop.plv.upload.invalid_format", "Un fichier .blend est attendu.");

        var userId = _currentUser.UserId
            ?? throw new DomainException("shop.plv.upload.no_user", "Utilisateur non authentifié.");

        // Id du job connu à l'avance pour nommer les fichiers de façon déterministe.
        var jobId = Guid.NewGuid();
        var blendPath = await _fileStore.SaveBlendAsync(jobId, request.BlendFileStream, request.FileName, ct);
        var job = StorageModelCompileJob.CreateForShop(jobId, shop.Id, blendPath, userId);

        await _jobRepo.AddAsync(job, ct);
        await _uow.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(job.Id, ct);

        return StorageMapper.ToDto(job);
    }
}
