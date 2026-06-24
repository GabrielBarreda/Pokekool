using Pokekool.Domain.Entities;

namespace Pokekool.Domain.Repositories;

public interface IProviderSettingRepository
{
    Task<ProviderSetting?> GetAsync(string provider, string key, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderSetting>> GetAllForProviderAsync(string provider, CancellationToken ct = default);
    Task UpsertAsync(ProviderSetting setting, CancellationToken ct = default);
}
