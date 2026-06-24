using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pokekool.Application.Abstractions;
using Pokekool.Infrastructure.DataProviders.CardMarket;
using Pokekool.Infrastructure.DataProviders.CardMarketScraping;
using Pokekool.Infrastructure.DataProviders.CardTrader;
using Pokekool.Infrastructure.DataProviders.Mavin;
using Pokekool.Infrastructure.DataProviders.PointSales;
using Pokekool.Infrastructure.DataProviders.TcgPlayer;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Authentication;
using Pokekool.Infrastructure.DataProviders.PokemonTcg;
using Pokekool.Infrastructure.DataProviders.PriceCharting;
using Pokekool.Infrastructure.DataProviders.Psa;
using Pokekool.Infrastructure.DataProviders.Scraping;
using Pokekool.Infrastructure.ExternalClients;
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

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(dbOptions.ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);

        services.AddDbContext<PokekoolDbContext>(opt =>
        {
            opt.UseNpgsql(dataSource, npgsql =>
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

        services.AddScoped<IPokemonSeriesRepository, PokemonSeriesRepository>();
        services.AddScoped<IPokemonSetRepository, PokemonSetRepository>();
        services.AddScoped<IPokemonCardRepository, PokemonCardRepository>();
        services.AddScoped<ICardMarketPriceRepository, CardMarketPriceRepository>();
        services.AddScoped<ICardPopulationRepository, CardPopulationRepository>();
        services.AddScoped<IProviderSettingRepository, ProviderSettingRepository>();

        // ---- PokemonTCG HTTP client (bas niveau, utilisé par PokemonTcgProvider) ----
        services.Configure<PokemonTcgOptions>(config.GetSection(PokemonTcgOptions.SectionName));
        services.AddHttpClient<IPokemonTcgClient, PokemonTcgClient>()
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            });

        // ---- ProviderSettingsService (chiffrement + cache) ----
        services.AddMemoryCache();
        services.AddDataProtection();
        services.AddScoped<IProviderSettingsService, ProviderSettingsService>();

        // ---- Providers (injectés via IEnumerable<T> dans DataAggregator) ----
        services.AddScoped<ICardMetadataProvider, PokemonTcgProvider>();
        services.AddScoped<ICardPriceProvider, PriceChartingProvider>();
        services.AddScoped<ICardPriceProvider, CardMarketProvider>();
        services.AddScoped<ICardPriceProvider, CardMarketScrapingProvider>();
        services.AddScoped<ICardPriceProvider, TcgPlayerProvider>();
        services.AddScoped<ICardPriceProvider, CardTraderProvider>();
        services.AddScoped<ICardPriceProvider, MavinProvider>();
        services.AddScoped<ICardPriceProvider, PointSalesProvider>();
        services.AddScoped<IPopulationProvider, PsaProvider>();

        // ---- CardMarket HttpClient (OAuth 1.0a via DelegatingHandler) ----
        services.Configure<CardMarketOptions>(config.GetSection(CardMarketOptions.SectionName));
        services.AddScoped<CardMarketAuthHandler>();
        services.AddHttpClient<ICardMarketClient, CardMarketClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CardMarketOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<CardMarketAuthHandler>();

        // ---- TCGPlayer HttpClient (OAuth 2.0 CC via TcgPlayerTokenHandler) ----
        services.Configure<TcgPlayerOptions>(config.GetSection(TcgPlayerOptions.SectionName));
        services.AddScoped<TcgPlayerTokenHandler>();
        services.AddHttpClient<ITcgPlayerClient, TcgPlayerClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TcgPlayerOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<TcgPlayerTokenHandler>();

        // ---- Aggregator ----
        services.AddScoped<DataAggregator>();

        // ---- Scraping pipeline (Playwright + Ollama) ----
        services.Configure<OllamaOptions>(config.GetSection(OllamaOptions.SectionName));
        services.AddSingleton<IWebScraper, PlaywrightScraper>();
        services.AddScoped<ILlmExtractor, OllamaLlmExtractor>();
        services.AddScoped<ScrapingPipeline>();

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
