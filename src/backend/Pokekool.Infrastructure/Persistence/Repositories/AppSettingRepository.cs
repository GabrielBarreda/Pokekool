using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class AppSettingRepository : IAppSettingRepository
{
    private readonly PokekoolDbContext _db;
    public AppSettingRepository(PokekoolDbContext db) => _db = db;

    public Task<AppSetting?> GetByKeyAsync(string key, CancellationToken ct = default)
        => _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);

    public async Task AddAsync(AppSetting setting, CancellationToken ct = default)
        => await _db.AppSettings.AddAsync(setting, ct);
}
