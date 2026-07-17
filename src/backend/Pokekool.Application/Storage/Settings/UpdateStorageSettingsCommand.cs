using MediatR;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Storage.Settings;

/// <summary>Met à jour les paramètres du module Stockage (upsert dans AppSetting).</summary>
public sealed record UpdateStorageSettingsCommand(bool CascadeDelete) : IRequest;

internal sealed class UpdateStorageSettingsCommandHandler : IRequestHandler<UpdateStorageSettingsCommand>
{
    private readonly IAppSettingRepository _settings;
    private readonly IUnitOfWork _uow;

    public UpdateStorageSettingsCommandHandler(IAppSettingRepository settings, IUnitOfWork uow)
    {
        _settings = settings;
        _uow = uow;
    }

    public async Task Handle(UpdateStorageSettingsCommand request, CancellationToken ct)
    {
        await UpsertAsync(StorageSettingsKeys.CascadeDelete, request.CascadeDelete ? "true" : "false", ct);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task UpsertAsync(string key, string value, CancellationToken ct)
    {
        var existing = await _settings.GetByKeyAsync(key, ct);
        if (existing is null) await _settings.AddAsync(AppSetting.Create(key, value), ct);
        else existing.SetValue(value);
    }
}
