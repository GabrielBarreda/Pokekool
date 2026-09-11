using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Pgvector.EntityFrameworkCore;

namespace Pokekool.Infrastructure.Persistence;

/// <summary>
/// Factory design-time explicite pour `dotnet ef migrations add` / `database update`.
///
/// Nécessaire depuis l'ajout du matching visuel (DINOv2, 2026-09-07, voir
/// Automatisations-LLM.md) : la shadow property "Embedding" (type Pgvector.Vector, colonne
/// vector(384)) exige que le mapping pgvector soit enregistré via UseVector() — à la fois côté
/// NpgsqlDataSourceBuilder ET côté NpgsqlDbContextOptionsBuilder — avant que EF construise le
/// modèle. En passant par AddInfrastructure()/Program.cs, les EF Tools ratent apparemment ce
/// mapping au design-time (host building non strictement identique au runtime réel), d'où
/// l'erreur "database provider does not support mapping 'Vector' properties". Cette factory
/// contourne le problème en construisant explicitement un DbContext déjà correctement configuré,
/// sans dépendre de la résolution DI complète de l'application.
/// </summary>
public sealed class PokekoolDbContextFactory : IDesignTimeDbContextFactory<PokekoolDbContext>
{
    public PokekoolDbContext CreateDbContext(string[] args)
    {
        // appsettings.json du projet de démarrage (Pokekool.API) — même source que le runtime.
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Pokekool.API");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(basePath) ? basePath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetSection("Database")["ConnectionString"]
            ?? "Host=localhost;Port=5434;Database=pokekool;Username=pokekool;Password=pokekool_dev_password";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<PokekoolDbContext>();
        optionsBuilder.UseNpgsql(dataSource, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(PokekoolDbContext).Assembly.FullName);
            npgsql.UseVector();
        });

        return new PokekoolDbContext(optionsBuilder.Options);
    }
}
