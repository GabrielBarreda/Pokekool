namespace Pokekool.Domain.Repositories;

/// <summary>
/// Validation atomique des changements faits via les repositories.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
