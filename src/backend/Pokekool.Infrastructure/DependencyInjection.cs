using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Chat;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Infrastructure.Chat;
using Pokekool.Infrastructure.Chat.Tools;
using Pokekool.Infrastructure.Collection.Scanning;
using Pokekool.Infrastructure.DataProviders.CardMarket;
using Pokekool.Infrastructure.DataProviders.CardMarketScraping;
using Pokekool.Infrastructure.DataProviders.GemTracker;
using Pokekool.Infrastructure.DataProviders.CardLadder;
using Pokekool.Infrastructure.DataProviders.CardTrader;
using Pokekool.Infrastructure.DataProviders.Mavin;
using Pokekool.Infrastructure.DataProviders.PointSales;
using Pokekool.Infrastructure.DataProviders.TcgPlayer;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Authentication;
using Pokekool.Infrastructure.BackgroundServices;
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
        services.AddScoped<ICardGradedPriceRepository, CardGradedPriceRepository>();
        services.AddScoped<ICardPopulationRepository, CardPopulationRepository>();
        services.AddScoped<IProviderMappingRepository, ProviderMappingRepository>();
        services.AddScoped<IProviderSettingRepository, ProviderSettingRepository>();
        services.AddScoped<IUserCollectionRepository, UserCollectionRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // ---- Module Boutique ----
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IShopProductRepository, ShopProductRepository>();
        services.AddScoped<IShopStockRepository, ShopStockRepository>();
        services.AddScoped<IShopAppointmentRepository, ShopAppointmentRepository>();

        // ---- Module Client ----
        services.AddScoped<IPurchaseRequestRepository, PurchaseRequestRepository>();
        services.AddScoped<IShopClientRepository, ShopClientRepository>();

        // ---- Module Stockage Physique ----
        services.AddScoped<Pokekool.Domain.Repositories.Storage.IStorageRoomRepository,
            Pokekool.Infrastructure.Persistence.Repositories.Storage.StorageRoomRepository>();
        services.AddScoped<Pokekool.Domain.Repositories.Storage.IStorageBoxRepository,
            Pokekool.Infrastructure.Persistence.Repositories.Storage.StorageBoxRepository>();
        services.AddScoped<Pokekool.Domain.Repositories.Storage.IStorageBinderRepository,
            Pokekool.Infrastructure.Persistence.Repositories.Storage.StorageBinderRepository>();
        services.AddScoped<Pokekool.Domain.Repositories.Storage.IStorageLocationRepository,
            Pokekool.Infrastructure.Persistence.Repositories.Storage.StorageLocationRepository>();
        services.AddScoped<Pokekool.Domain.Repositories.Storage.IStorageMovementEventRepository,
            Pokekool.Infrastructure.Persistence.Repositories.Storage.StorageMovementEventRepository>();
        services.AddScoped<Pokekool.Domain.Repositories.Storage.IStorageModelCompileJobRepository,
            Pokekool.Infrastructure.Persistence.Repositories.Storage.StorageModelCompileJobRepository>();

        // Pipeline Blender (.blend → .glb)
        services.Configure<Pokekool.Infrastructure.Options.BlenderOptions>(
            config.GetSection(Pokekool.Infrastructure.Options.BlenderOptions.SectionName));
        services.AddScoped<Pokekool.Application.Storage.IBlenderCompiler,
            Pokekool.Infrastructure.Storage.BlenderCliCompiler>();
        services.AddScoped<Pokekool.Application.Storage.IBlenderFileStore,
            Pokekool.Infrastructure.Storage.BlenderFileStore>();
        // File d'attente singleton, partagée entre le handler d'upload et le background service.
        services.AddSingleton<Pokekool.Infrastructure.Storage.InMemoryCompileQueue>();
        services.AddSingleton<Pokekool.Application.Storage.IStorageModelCompileQueue>(
            sp => sp.GetRequiredService<Pokekool.Infrastructure.Storage.InMemoryCompileQueue>());
        services.AddHostedService<BlenderCompileBackgroundService>();

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
        // CardMarketScraping désactivé : bot-bloqué (HTML 371 chars), appliquer SemaphoreSlim + stealth avant de réactiver.
        // services.AddScoped<ICardPriceProvider, CardMarketScrapingProvider>();
        services.AddScoped<ICardPriceProvider, TcgPlayerProvider>();
        services.AddScoped<ICardPriceProvider, CardTraderProvider>();
        // Mavin désactivé : domaine mavin.io en vente (afternic), provider inutilisable.
        // services.AddScoped<ICardPriceProvider, MavinProvider>();
        services.AddScoped<ICardPriceProvider, PointSalesProvider>();
        services.AddScoped<ICardPriceProvider, CardLadderProvider>();

        // ---- CardLadder Firestore client (HttpClient, public API — no auth required) ----
        services.AddHttpClient<CardLadderFirestoreClient>(client =>
        {
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IPopulationProvider, PsaProvider>();

        // GemTracker : HttpClient dédié pour la discovery SSR (plain HTTP, pas de Playwright)
        services.AddHttpClient("gemtracker", client =>
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IPopulationProvider, GemTrackerProvider>();

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

        // ---- Sync progress + schedule (singletons) ----
        services.AddSingleton<SyncProgressService>();
        services.AddSingleton<ISyncProgressReporter>(sp => sp.GetRequiredService<SyncProgressService>());
        services.AddSingleton<SyncScheduleState>();
        services.AddHostedService<ScheduledSyncService>();

        // ---- DbConcurrencyGuard — sémaphore scoped partagé entre providers ----
        // Sérialise les accès EF Core quand les providers tournent en parallèle.
        services.AddScoped<DbConcurrencyGuard>();

        // ---- Aggregator ----
        services.AddScoped<DataAggregator>();

        // ---- Scraping pipeline (Playwright + Ollama) ----
        services.Configure<OllamaOptions>(config.GetSection(OllamaOptions.SectionName));
        services.AddSingleton<IWebScraper, PlaywrightScraper>();
        services.AddScoped<ILlmExtractor, OllamaLlmExtractor>();
        services.AddScoped<ScrapingPipeline>();

        // ---- Scan Collection (Vision LLM + Vault RAG) ----
        services.Configure<OllamaVisionOptions>(config.GetSection(OllamaVisionOptions.SectionName));
        services.AddSingleton<ICardVisionAnalyzer, OllamaCardVisionAnalyzer>();
        services.AddSingleton<IVaultKnowledgeService, VaultKnowledgeService>();

        services.AddScoped<DbInitializer>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();
        services.AddScoped<IPermissionResolver, RedisPermissionResolver>();

        // ---- ChatBot IA (function calling via Ollama /api/chat) ----
        services.Configure<OllamaChatOptions>(config.GetSection(OllamaChatOptions.SectionName));
        services.AddScoped<IOllamaChatClient, OllamaChatClient>();
        services.AddScoped<RedisChatSessionStore>();
        services.AddScoped<IMarketPriceResearcher, EbayMarketPriceResearcher>();
        services.AddScoped<IChatOrchestrator, ChatOrchestrator>();

        // Outils de lecture (pas de confirmation requise)
        services.AddScoped<IChatTool, SearchStockTool>();
        services.AddScoped<IChatTool, SearchProductsTool>();
        services.AddScoped<IChatTool, GetPlanningTool>();
        services.AddScoped<IChatTool, GetMarketPriceTool>();
        services.AddScoped<IChatTool, OpenCameraTool>();

        // Outils d'action (human-in-the-loop)
        services.AddScoped<IChatTool, CreateStockActionTool>();
        services.AddScoped<IChatTool, CreateProductActionTool>();
        services.AddScoped<IChatTool, UpdateAppointmentStatusActionTool>();
        services.AddScoped<IChatTool, RescheduleAppointmentActionTool>();

        return services;
    }

    public static IServiceCollection AddDbInitializerOnStartup(this IServiceCollection services)
    {
        services.AddHostedService<DbInitializerHostedService>();
        return services;
    }
}
