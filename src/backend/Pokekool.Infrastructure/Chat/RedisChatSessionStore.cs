using System.Text.Json;
using Microsoft.Extensions.Options;
using Pokekool.Application.Chat;
using Pokekool.Infrastructure.Options;
using StackExchange.Redis;

namespace Pokekool.Infrastructure.Chat;

/// <summary>Persistance des sessions chatbot dans Redis (déjà enregistré pour les refresh tokens).</summary>
public sealed class RedisChatSessionStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly OllamaChatOptions _options;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public RedisChatSessionStore(IConnectionMultiplexer redis, IOptions<OllamaChatOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    private static string Key(string sessionId) => $"chat:session:{sessionId}";
    private TimeSpan Ttl => TimeSpan.FromMinutes(_options.SessionTtlMinutes);

    public async Task<ChatSession?> GetAsync(string sessionId)
    {
        var db = _redis.GetDatabase();
        var raw = await db.StringGetAsync(Key(sessionId));
        return raw.IsNullOrEmpty ? null : JsonSerializer.Deserialize<ChatSession>((string)raw!, _json);
    }

    public async Task SaveAsync(ChatSession session)
    {
        var db = _redis.GetDatabase();
        var raw = JsonSerializer.Serialize(session);
        await db.StringSetAsync(Key(session.Id), raw, Ttl);
    }
}
