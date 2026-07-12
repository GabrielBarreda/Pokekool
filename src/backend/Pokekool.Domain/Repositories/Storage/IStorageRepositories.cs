using Pokekool.Domain.Entities.Storage;

namespace Pokekool.Domain.Repositories.Storage;

public interface IStorageRoomRepository
{
    Task<StorageRoom?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StorageRoom>> ListByShopAsync(Guid shopId, CancellationToken ct = default);
    Task AddAsync(StorageRoom room, CancellationToken ct = default);
}

public interface IStorageFurnitureRepository
{
    Task<StorageFurniture?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StorageFurniture>> ListByRoomAsync(Guid roomId, CancellationToken ct = default);
    Task AddAsync(StorageFurniture furniture, CancellationToken ct = default);
    void Remove(StorageFurniture furniture);
}

public interface IStorageBoxRepository
{
    Task<StorageBox?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StorageBox>> ListByRoomAsync(Guid roomId, LabelOrigin? labelOrigin, CancellationToken ct = default);

    /// <summary>Recherche un carton par code-barre OU code interne (lookup "scan théorique").</summary>
    Task<StorageBox?> FindByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Cartons rangés sur un meuble donné (pour détacher à la suppression du meuble).</summary>
    Task<IReadOnlyList<StorageBox>> ListByFurnitureAsync(Guid furnitureId, CancellationToken ct = default);
    Task AddAsync(StorageBox box, CancellationToken ct = default);
}

public interface IStorageBinderRepository
{
    Task<StorageBinder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StorageBinder>> ListByBoxAsync(Guid boxId, CancellationToken ct = default);
    Task<IReadOnlyList<StorageBinder>> ListByBoxIdsAsync(IReadOnlyList<Guid> boxIds, CancellationToken ct = default);

    /// <summary>Classeurs posés directement sur un meuble donné.</summary>
    Task<IReadOnlyList<StorageBinder>> ListByFurnitureAsync(Guid furnitureId, CancellationToken ct = default);
    Task AddAsync(StorageBinder binder, CancellationToken ct = default);
}

public interface IStorageLocationRepository
{
    Task<IReadOnlyList<StorageLocation>> ListByBinderAsync(Guid binderId, CancellationToken ct = default);
    Task<IReadOnlyList<StorageLocation>> ListByBinderIdsAsync(IReadOnlyList<Guid> binderIds, CancellationToken ct = default);
    Task AddAsync(StorageLocation location, CancellationToken ct = default);
}

public interface IStorageMovementEventRepository
{
    Task<StorageMovementEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StorageMovementEvent>> ListPendingAsync(CancellationToken ct = default);
    Task AddAsync(StorageMovementEvent movement, CancellationToken ct = default);
}

public interface IStorageModelCompileJobRepository
{
    Task<StorageModelCompileJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(StorageModelCompileJob job, CancellationToken ct = default);
}
