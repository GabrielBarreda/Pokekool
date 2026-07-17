using Pokekool.Domain.Entities;

namespace Pokekool.Domain.Repositories;

/// <summary>Accès aux paramètres applicatifs génériques (clé/valeur non chiffrés).</summary>
public interface IAppSettingRepository
{
    Task<AppSetting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task AddAsync(AppSetting setting, CancellationToken ct = default);
}
