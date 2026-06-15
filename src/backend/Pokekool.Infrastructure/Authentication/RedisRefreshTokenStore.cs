using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pokekool.Application.Abstractions;
using Pokekool.Infrastructure.Options;
using StackExchange.Redis;

namespace Pokekool.Infrastructure.Authentication;

/// <summary>
/// Stockage Redis des refresh tokens.
///
/// Clé : {InstanceName}refresh:{sha256(token)}
/// Valeur : JSON {UserId, TenantId, ExpiresAt}
///
/// On stocke le HASH du token, pas le token brut, pour qu'un dump Redis ne
/// donne pas accès aux tokens utilisables tels quels (defense-in-depth).
/// </summary>
internal sealed class RedisRefreshTokenStore : IRefreshTokenStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _opt;

    public RedisRefreshTokenStore(IConnectionMultiplexer redis, IOptions<RedisOptions> opt)
    {
        _redis = redis;
        _opt = opt.Value;
    }

    public async Task SaveAsync(string token, Guid userId, Guid? tenantId, TimeSpan ttl, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(token);
        var payload = JsonSerializer.Serialize(
            new RefreshTokenData(userId, tenantId, DateTimeOffset.UtcNow.Add(ttl)),
            JsonOpts);
        await db.StringSetAsync(key, payload, ttl).ConfigureAwait(false);
    }

    public async Task<RefreshTokenData?> GetAsync(string token, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var raw = await db.StringGetAsync(BuildKey(token)).ConfigureAwait(false);
        if (raw.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<RefreshTokenData>((string)raw!, JsonOpts);
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(BuildKey(token)).ConfigureAwait(false);
    }

    private string BuildKey(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var hex = Convert.ToHexString(hash);
        return $"{_opt.InstanceName}refresh:{hex}";
    }
}
