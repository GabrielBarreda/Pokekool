using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly PokekoolDbContext _db;

    public UnitOfWork(PokekoolDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
