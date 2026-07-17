using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Settings;

/// <summary>Lit les paramètres du module Stockage (valeurs par défaut si absents en base).</summary>
public sealed record GetStorageSettingsQuery : IRequest<StorageSettingsDto>;

internal sealed class GetStorageSettingsQueryHandler
    : IRequestHandler<GetStorageSettingsQuery, StorageSettingsDto>
{
    private readonly IAppSettingRepository _settings;

    public GetStorageSettingsQueryHandler(IAppSettingRepository settings) => _settings = settings;

    public async Task<StorageSettingsDto> Handle(GetStorageSettingsQuery request, CancellationToken ct)
    {
        var cascade = await StorageSettingsReader.GetCascadeDeleteAsync(_settings, ct);
        return new StorageSettingsDto(cascade);
    }
}

/// <summary>Helper partagé : lecture typée du flag de cascade (réutilisé par les handlers de suppression).</summary>
internal static class StorageSettingsReader
{
    public static async Task<bool> GetCascadeDeleteAsync(IAppSettingRepository settings, CancellationToken ct)
    {
        var entry = await settings.GetByKeyAsync(StorageSettingsKeys.CascadeDelete, ct);
        if (entry is null) return StorageSettingsKeys.CascadeDeleteDefault;
        return bool.TryParse(entry.Value, out var v) ? v : StorageSettingsKeys.CascadeDeleteDefault;
    }
}
