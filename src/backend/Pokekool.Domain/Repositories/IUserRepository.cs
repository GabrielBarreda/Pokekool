using Pokekool.Domain.Entities;
using Pokekool.Domain.ValueObjects;

namespace Pokekool.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListAsync(bool includeInactive = false, CancellationToken ct = default);
    /// <summary>
    /// Liste paginée avec filtres optionnels. <paramref name="roleId"/> est déjà résolu depuis le code.
    /// </summary>
    Task<(IReadOnlyList<User> Items, long TotalCount)> GetPagedAsync(
        string? search,
        Guid? roleId,
        bool? recentOnly,
        string? sortBy,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);
}
