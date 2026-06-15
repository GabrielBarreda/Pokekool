namespace Pokekool.Application.Abstractions;

/// <summary>
/// Stockage Redis des refresh tokens. Permet la révocation instantanée.
/// </summary>
public interface IRefreshTokenStore
{
    Task SaveAsync(string token, Guid userId, Guid? tenantId, TimeSpan ttl, CancellationToken ct = default);
    Task<RefreshTokenData?> GetAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);
}

public sealed record RefreshTokenData(Guid UserId, Guid? TenantId, DateTimeOffset ExpiresAt);
