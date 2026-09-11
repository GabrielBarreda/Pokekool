using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Collection;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class ScanRunRepository : IScanRunRepository
{
    private readonly PokekoolDbContext _db;

    public ScanRunRepository(PokekoolDbContext db) => _db = db;

    public async Task AddAsync(ScanRun scanRun, CancellationToken ct = default)
        => await _db.ScanRuns.AddAsync(scanRun, ct);

    public async Task AddItemAsync(ScanRunItem item, CancellationToken ct = default)
        => await _db.ScanRunItems.AddAsync(item, ct);

    public Task<ScanRun?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ScanRuns.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ScanRun>> ListSummariesAsync(CancellationToken ct = default)
        => await _db.ScanRuns
            .OrderByDescending(r => r.RunAt)
            .ToListAsync(ct);

    public Task<ScanRun?> GetWithItemsAsync(Guid id, CancellationToken ct = default)
        => _db.ScanRuns
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<(int TotalPhotos, int DirectMatches, int ManualMatches, int Failures)> GetConfidenceStatsAsync(
        CancellationToken ct = default)
    {
        // _excludedCardIds est mappé en champ privé (uuid[], voir ScanRunItemConfiguration) — on y
        // accède via EF.Property pour rester traduisible en SQL (cardinality(...) > 0) plutôt que de
        // charger les entités en mémoire.
        var query = _db.ScanRunItems.Select(i => new
        {
            i.Success,
            HasExclusions = EF.Property<List<Guid>>(i, "_excludedCardIds").Count > 0,
        });

        var totalPhotos = await query.CountAsync(ct);
        var directMatches = await query.CountAsync(i => i.Success && !i.HasExclusions, ct);
        var manualMatches = await query.CountAsync(i => i.Success && i.HasExclusions, ct);
        var failures = await query.CountAsync(i => !i.Success, ct);

        return (totalPhotos, directMatches, manualMatches, failures);
    }
}
