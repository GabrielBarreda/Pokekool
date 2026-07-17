using MediatR;
using Pokekool.Application.Storage.Settings;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Supprime une salle. Selon le paramètre « suppression en chaîne » (AppSetting
/// <c>storage.cascade_delete</c>) :
/// <list type="bullet">
///   <item>ON (défaut) : par cascade FK, ses meubles / cartons / classeurs / localisations sont supprimés.</item>
///   <item>OFF : refuse la suppression tant que la salle n'est pas vide (un carton/meuble ne peut
///   exister sans salle — FK requise — donc pas de détachement possible : l'utilisateur doit d'abord vider la salle).</item>
/// </list>
/// </summary>
public sealed record DeleteStorageRoomCommand(Guid RoomId) : IRequest;

internal sealed class DeleteStorageRoomCommandHandler : IRequestHandler<DeleteStorageRoomCommand>
{
    private readonly IStorageRoomRepository _repo;
    private readonly IStorageBoxRepository _boxes;
    private readonly IStorageFurnitureRepository _furniture;
    private readonly IAppSettingRepository _settings;
    private readonly IUnitOfWork _uow;

    public DeleteStorageRoomCommandHandler(
        IStorageRoomRepository repo, IStorageBoxRepository boxes,
        IStorageFurnitureRepository furniture, IAppSettingRepository settings, IUnitOfWork uow)
    {
        _repo = repo;
        _boxes = boxes;
        _furniture = furniture;
        _settings = settings;
        _uow = uow;
    }

    public async Task Handle(DeleteStorageRoomCommand request, CancellationToken ct)
    {
        var room = await _repo.GetByIdAsync(request.RoomId, ct)
            ?? throw new DomainException("storage.room.not_found", "Salle introuvable.");

        var cascade = await StorageSettingsReader.GetCascadeDeleteAsync(_settings, ct);
        if (!cascade)
        {
            var boxes = await _boxes.ListByRoomAsync(room.Id, null, ct);
            var furniture = await _furniture.ListByRoomAsync(room.Id, ct);
            if (boxes.Count > 0 || furniture.Count > 0)
                throw new DomainException(
                    "storage.room.not_empty",
                    "Suppression en chaîne désactivée : videz d'abord la salle (cartons et meubles) avant de la supprimer.");
        }

        _repo.Remove(room);
        await _uow.SaveChangesAsync(ct);
    }
}
