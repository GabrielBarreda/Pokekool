using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pokekool.Infrastructure.Persistence.Design;

/// <summary>
/// Factory utilisée uniquement à design-time par les commandes dotnet ef.
/// Elle évite d'avoir à démarrer toute la pipeline ASP.NET pour générer des migrations.
///
/// Commande type (depuis src/backend) :
///   dotnet ef migrations add Initial \
///     --project Pokekool.Infrastructure \
///     --startup-project Pokekool.API \
///     --output-dir Persistence/Migrations
/// </summary>
public sealed class PokekoolDbContextFactory : IDesignTimeDbContextFactory<PokekoolDbContext>
{
    public PokekoolDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POKEKOOL_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=pokekool;Username=pokekool;Password=pokekool_dev_password";

        var options = new DbContextOptionsBuilder<PokekoolDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PokekoolDbContext).Assembly.FullName);
            })
            .Options;

        return new PokekoolDbContext(options);
    }
}
