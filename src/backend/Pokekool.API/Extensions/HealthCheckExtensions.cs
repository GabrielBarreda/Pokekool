using System.Text.Json;
using HealthChecks.NpgSql;
using HealthChecks.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Pokekool.Infrastructure.Options;

namespace Pokekool.API.Extensions;

public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly string[] DbTags = { "ready", "db" };
    private static readonly string[] CacheTags = { "ready", "cache" };

    public static IServiceCollection AddPokekoolHealthChecks(this IServiceCollection services, IConfiguration config)
    {
        var dbOptions = config.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();
        var redisOptions = config.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
            ?? new RedisOptions();

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: dbOptions.ConnectionString,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: DbTags)
            .AddRedis(
                redisConnectionString: redisOptions.ConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: CacheTags);

        return services;
    }

    public static WebApplication MapPokekoolHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/healthz/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteJsonResponse
        });

        app.MapHealthChecks("/healthz/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteJsonResponse
        });

        app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteJsonResponse
        });

        return app;
    }

    private static Task WriteJsonResponse(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = e.Value.Exception?.Message,
                tags = e.Value.Tags
            })
        };

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
