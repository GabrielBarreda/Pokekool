using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// L'admin uploade un projet Blender (.blend). On persiste le fichier, on crée un job
/// de compilation (status=queued) et on l'enfile. La compilation .blend → .glb est
/// asynchrone (cf. <c>BlenderCompileBackgroundService</c>) ; le front poll le statut.
/// </summary>
public sealed record UploadRoomModelCommand(
    Guid RoomId,
    Stream BlendFileStream,
    string FileName) : IRequest<StorageCompileJobDto>;

internal sealed class UploadRoomModelCommandHandler
    : IRequestHandler<UploadRoomModelCommand, StorageCompileJobDto>
{
    private readonly IStorageRoomRepository _roomRepo;
    private readonly IStorageModelCompileJobRepository _jobRepo;
    private readonly IBlenderFileStore _fileStore;
    private readonly IStorageModelCompileQueue _queue;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public UploadRoomModelCommandHandler(
        IStorageRoomRepository roomRepo,
        IStorageModelCompileJobRepository jobRepo,
        IBlenderFileStore fileStore,
        IStorageModelCompileQueue queue,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _roomRepo = roomRepo;
        _jobRepo = jobRepo;
        _fileStore = fileStore;
        _queue = queue;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<StorageCompileJobDto> Handle(UploadRoomModelCommand request, CancellationToken ct)
    {
        var room = await _roomRepo.GetByIdAsync(request.RoomId, ct)
            ?? throw new DomainException("storage.room.not_found", "Salle de stockage introuvable.");

        if (!request.FileName.EndsWith(".blend", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("storage.upload.invalid_format", "Un fichier .blend est attendu.");

        var userId = _currentUser.UserId
            ?? throw new DomainException("storage.upload.no_user", "Utilisateur non authentifié.");

        // Id du job connu à l'avance pour nommer les fichiers de façon déterministe.
        var jobId = Guid.NewGuid();
        var blendPath = await _fileStore.SaveBlendAsync(jobId, request.BlendFileStream, request.FileName, ct);
        var job = StorageModelCompileJob.CreateForRoom(jobId, room.Id, blendPath, userId);

        await _jobRepo.AddAsync(job, ct);
        await _uow.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(job.Id, ct);

        return StorageMapper.ToDto(job);
    }
}
