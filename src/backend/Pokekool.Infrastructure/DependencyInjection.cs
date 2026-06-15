using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Authentication;
using Pokekool.Infrastructure.Options;
using Pokekool.Infrastructure.Persistence;
using Pokekool.Infrastructure.Persistence.Repositories;
using Pokekool.Infrastructure.Services;
using StackExchange.Redis;

namespace Pokekool.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DatabaseOptions>(config.GetSection(DatabaseOptions.SectionName));
        services.Configure<RedisOptions>(config.GetSection(RedisOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        var dbOptions = config.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();

        services.AddDbContext<PokekoolDbContext>(opt =>
        {
            opt.UseNpgsql(dbOptions.ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PokekoolDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            });
            if (dbOptions.EnableSensitiveDataLogging) opt.EnableSensitiveDataLogging();
            if (dbOptions.EnableDetailedErrors) opt.EnableDetailedErrors();
        });

        var redisOptions = config.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
            ?? new RedisOptions();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisOptions.ConnectionString));

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<DbInitializer>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();
        services.AddScoped<IPermissionResolver, RedisPermissionResolver>();

        return services;
    }

    public static IServiceCollection AddDbInitializerOnStartup(this IServiceCollection services)
    {
        services.AddHostedService<DbInitializerHostedService>();
        return services;
    }
}
