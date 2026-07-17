using MediatR;
using Pokekool.Application.Storage.Settings;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;

namespace Pokekool.Application.Storage.Commands;

/// <summary>
/// Supprime un carton. Selon le paramètre « suppression en chaîne » (AppSetting
/// <c>storage.cascade_delete</c>) :
/// <list type="bullet">
///   <item>ON (défaut) : la FK carton→classeur (ON DELETE CASCADE) supprime aussi les classeurs et leurs localisations.</item>
///   <item>OFF : les classeurs du carton sont d'abord <b>détachés</b> (deviennent « non rangés », conservés en base) avant suppression du carton.</item>
/// </list>
/// </summary>
public sealed record DeleteStorageBoxCommand(Guid BoxId) : IRequest;

internal sealed class DeleteStorageBoxCommandHandler : IRequestHandler<DeleteStorageBoxCommand>
{
    private readonly IStorageBoxRepository _repo;
    private readonly IStorageBinderRepository _binders;
    private readonly IAppSettingRepository _settings;
    private readonly IUnitOfWork _uow;

    public DeleteStorageBoxCommandHandler(
        IStorageBoxRepository repo, IStorageBinderRepository binders,
        IAppSettingRepository settings, IUnitOfWork uow)
    {
        _repo = repo;
        _binders = binders;
        _settings = settings;
        _uow = uow;
    }

    public async Task Handle(DeleteStorageBoxCommand request, CancellationToken ct)
    {
        var box = await _repo.GetByIdAsync(request.BoxId, ct)
            ?? throw new DomainException("storage.box.not_found", "Carton introuvable.");

        var cascade = await StorageSettingsReader.GetCascadeDeleteAsync(_settings, ct);
        if (!cascade)
        {
            // Détache les classeurs pour ne pas les perdre par cascade FK.
            var binders = await _binders.ListByBoxAsync(box.Id, ct);
            foreach (var binder in binders) binder.Detach();
            await _uow.SaveChangesAsync(ct); // persiste le détachement avant la suppression du carton
        }

        _repo.Remove(box);
        await _uow.SaveChangesAsync(ct);
    }
}
