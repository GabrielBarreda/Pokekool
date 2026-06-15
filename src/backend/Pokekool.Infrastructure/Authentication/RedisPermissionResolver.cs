using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Options;
using Pokekool.Shared.Constants;
using StackExchange.Redis;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Pokekool.Infrastructure.Authentication;

/// <summary>
/// Implements IPermissionResolver with Redis cache per role.
/// </summary>
internal sealed class RedisPermissionResolver : IPermissionResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOpts = new();

    private readonly IHttpContextAccessor _accessor;
    private readonly IRoleRepository _roles;
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _redisOpt;

    public RedisPermissionResolver(
        IHttpContextAccessor accessor,
        IRoleRepository roles,
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> redisOpt)
    {
        _accessor = accessor;
        _roles = roles;
        _redis = redis;
        _redisOpt = redisOpt.Value;
    }

    public async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        var perms = await GetPermissionsAsync(cancellationToken);
        return perms.Contains(permissionCode);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var user = _accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return Array.Empty<string>();

        var roleCodes = user.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
        if (roleCodes.Count == 0) return Array.Empty<string>();

        // SuperAdmin / Admin: bypass — all permissions.
        if (roleCodes.Contains(SystemRoles.SuperAdmin, StringComparer.Ordinal) ||
            roleCodes.Contains(SystemRoles.Admin, StringComparer.Ordinal))
            return Permissions.All.ToHashSet(StringComparer.Ordinal);

        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in roleCodes)
        {
            var perms = await ResolveRolePermissionsAsync(code, cancellationToken);
            foreach (var p in perms) result.Add(p);
        }
        return result;
    }

    public async Task InvalidateRoleCacheAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roles.GetByIdAsync(roleId, cancellationToken);
        if (role is null) return;

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(BuildKey(role.Code)).ConfigureAwait(false);
    }

    private async Task<IReadOnlyCollection<string>> ResolveRolePermissionsAsync(string roleCode, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(roleCode);

        var cached = await db.StringGetAsync(key).ConfigureAwait(false);
        if (!cached.IsNullOrEmpty)
        {
            var fromCache = JsonSerializer.Deserialize<string[]>((string)cached!, JsonOpts);
            if (fromCache is not null) return fromCache;
        }

        var role = await _roles.GetByCodeAsync(null, roleCode, cancellationToken);
        var perms = role?.PermissionCodes.ToArray() ?? Array.Empty<string>();

        await db.StringSetAsync(key, JsonSerializer.Serialize(perms, JsonOpts), CacheTtl).ConfigureAwait(false);
        return perms;
    }

    private string BuildKey(string roleCode)
        => $"{_redisOpt.InstanceName}perms:role:global:{roleCode}";
}
