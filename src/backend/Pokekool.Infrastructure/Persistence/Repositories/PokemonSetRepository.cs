using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class PokemonSetRepository : IPokemonSetRepository
{
    private readonly PokekoolDbContext _db;

    public PokemonSetRepository(PokekoolDbContext db) => _db = db;

    public Task<PokemonSet?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PokemonSets
            .Include(s => s.Cards)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<PokemonSet?> GetByTcgIdAsync(string tcgId, CancellationToken ct = default)
        => _db.PokemonSets
            .Include(s => s.Cards)
            .FirstOrDefaultAsync(s => s.TcgId == tcgId, ct);

    public async Task<IReadOnlyList<PokemonSet>> ListAsync(CancellationToken ct = default)
        => await _db.PokemonSets
            .Include(s => s.Cards)
            .OrderBy(s => s.ReleaseDate)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task AddAsync(PokemonSet set, CancellationToken ct = default)
        => await _db.PokemonSets.AddAsync(set, ct);
}
