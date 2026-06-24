using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class PokemonSeriesRepository : IPokemonSeriesRepository
{
    private readonly PokekoolDbContext _db;

    public PokemonSeriesRepository(PokekoolDbContext db) => _db = db;

    public Task<PokemonSeries?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PokemonSeries
            .Include(s => s.Sets)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<PokemonSeries?> GetByTcgIdAsync(string tcgId, CancellationToken ct = default)
        => _db.PokemonSeries
            .Include(s => s.Sets)
            .FirstOrDefaultAsync(s => s.TcgId == tcgId, ct);

    public async Task<IReadOnlyList<PokemonSeries>> ListAsync(CancellationToken ct = default)
        => await _db.PokemonSeries
            .Include(s => s.Sets)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task AddAsync(PokemonSeries series, CancellationToken ct = default)
        => await _db.PokemonSeries.AddAsync(series, ct);
}
