using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories.Storage;

namespace Pokekool.Infrastructure.Persistence.Repositories.Storage;

internal sealed class StorageRoomRepository : IStorageRoomRepository
{
    private readonly PokekoolDbContext _db;
    public StorageRoomRepository(PokekoolDbContext db) => _db = db;

    public Task<StorageRoom?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StorageRooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<StorageRoom>> ListByShopAsync(Guid shopId, CancellationToken ct = default)
        => await _db.StorageRooms.Where(r => r.ShopId == shopId)
            .OrderBy(r => r.Name).ToListAsync(ct);

    public async Task AddAsync(StorageRoom room, CancellationToken ct = default)
        => await _db.StorageRooms.AddAsync(room, ct);

    public void Remove(StorageRoom room) => _db.StorageRooms.Remove(room);
}

internal sealed class StorageBoxRepository : IStorageBoxRepository
{
    private readonly PokekoolDbContext _db;
    public StorageBoxRepository(PokekoolDbContext db) => _db = db;

    public Task<StorageBox?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StorageBoxes.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<StorageBox>> ListByRoomAsync(
        Guid roomId, LabelOrigin? labelOrigin, CancellationToken ct = default)
    {
        var query = _db.StorageBoxes.Where(b => b.RoomId == roomId);
        if (labelOrigin.HasValue) query = query.Where(b => b.LabelOrigin == labelOrigin.Value);
        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync(ct);
    }

    public Task<StorageBox?> FindByCodeAsync(string code, CancellationToken ct = default)
        => _db.StorageBoxes.FirstOrDefaultAsync(
            b => b.Barcode == code || b.InternalCode == code, ct);

    public async Task<IReadOnlyList<StorageBox>> ListByFurnitureAsync(Guid furnitureId, CancellationToken ct = default)
        => await _db.StorageBoxes.Where(b => b.FurnitureId == furnitureId).ToListAsync(ct);

    public async Task AddAsync(StorageBox box, CancellationToken ct = default)
        => await _db.StorageBoxes.AddAsync(box, ct);

    public void Remove(StorageBox box) => _db.StorageBoxes.Remove(box);
}

internal sealed class StorageBinderRepository : IStorageBinderRepository
{
    private readonly PokekoolDbContext _db;
    public StorageBinderRepository(PokekoolDbContext db) => _db = db;

    public Task<StorageBinder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StorageBinders.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<StorageBinder>> ListByBoxAsync(Guid boxId, CancellationToken ct = default)
        => await _db.StorageBinders.Where(b => b.BoxId == boxId)
            .OrderBy(b => b.PositionInBox).ThenBy(b => b.Label).ToListAsync(ct);

    public async Task<IReadOnlyList<StorageBinder>> ListByBoxIdsAsync(
        IReadOnlyList<Guid> boxIds, CancellationToken ct = default)
    {
        if (boxIds.Count == 0) return Array.Empty<StorageBinder>();
        return await _db.StorageBinders.Where(b => b.BoxId != null && boxIds.Contains(b.BoxId.Value))
            .OrderBy(b => b.PositionInBox).ThenBy(b => b.Label).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StorageBinder>> ListByFurnitureAsync(Guid furnitureId, CancellationToken ct = default)
        => await _db.StorageBinders.Where(b => b.FurnitureId == furnitureId)
            .OrderBy(b => b.Label).ToListAsync(ct);

    public async Task AddAsync(StorageBinder binder, CancellationToken ct = default)
        => await _db.StorageBinders.AddAsync(binder, ct);
}

internal sealed class StorageLocationRepository : IStorageLocationRepository
{
    private readonly PokekoolDbContext _db;
    public StorageLocationRepository(PokekoolDbContext db) => _db = db;

    public async Task<IReadOnlyList<StorageLocation>> ListByBinderAsync(Guid binderId, CancellationToken ct = default)
        => await _db.StorageLocations.Where(l => l.BinderId == binderId)
            .OrderBy(l => l.SlotPosition).ToListAsync(ct);

    public async Task<IReadOnlyList<StorageLocation>> ListByBinderIdsAsync(
        IReadOnlyList<Guid> binderIds, CancellationToken ct = default)
    {
        if (binderIds.Count == 0) return Array.Empty<StorageLocation>();
        return await _db.StorageLocations.Where(l => binderIds.Contains(l.BinderId))
            .OrderBy(l => l.SlotPosition).ToListAsync(ct);
    }

    public Task<StorageLocation?> FindByOwnerAsync(string ownerType, Guid ownerId, CancellationToken ct = default)
        => _db.StorageLocations.FirstOrDefaultAsync(
            l => l.OwnerType == ownerType && l.OwnerId == ownerId, ct);

    public async Task AddAsync(StorageLocation location, CancellationToken ct = default)
        => await _db.StorageLocations.AddAsync(location, ct);
}

internal sealed class StorageMovementEventRepository : IStorageMovementEventRepository
{
    private readonly PokekoolDbContext _db;
    public StorageMovementEventRepository(PokekoolDbContext db) => _db = db;

    public Task<StorageMovementEvent?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StorageMovementEvents.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<StorageMovementEvent>> ListPendingAsync(CancellationToken ct = default)
        => await _db.StorageMovementEvents
            .Where(m => m.Status == MovementStatus.Pending)
            .OrderByDescending(m => m.DetectedAt).ToListAsync(ct);

    public async Task AddAsync(StorageMovementEvent movement, CancellationToken ct = default)
        => await _db.StorageMovementEvents.AddAsync(movement, ct);
}

internal sealed class StorageModelCompileJobRepository : IStorageModelCompileJobRepository
{
    private readonly PokekoolDbContext _db;
    public StorageModelCompileJobRepository(PokekoolDbContext db) => _db = db;

    public Task<StorageModelCompileJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StorageModelCompileJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(StorageModelCompileJob job, CancellationToken ct = default)
        => await _db.StorageModelCompileJobs.AddAsync(job, ct);
}

internal sealed class StorageFurnitureRepository : IStorageFurnitureRepository
{
    private readonly PokekoolDbContext _db;
    public StorageFurnitureRepository(PokekoolDbContext db) => _db = db;

    public Task<StorageFurniture?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StorageFurnitures.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<StorageFurniture>> ListByRoomAsync(Guid roomId, CancellationToken ct = default)
        => await _db.StorageFurnitures.Where(f => f.RoomId == roomId)
            .OrderBy(f => f.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(StorageFurniture furniture, CancellationToken ct = default)
        => await _db.StorageFurnitures.AddAsync(furniture, ct);

    public void Remove(StorageFurniture furniture) => _db.StorageFurnitures.Remove(furniture);
}
