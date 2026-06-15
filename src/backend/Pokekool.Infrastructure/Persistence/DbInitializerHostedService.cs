using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Pokekool.Infrastructure.Persistence;

/// <summary>
/// HostedService qui exécute DbInitializer au démarrage de l'application.
/// Enregistré conditionnellement dans Program.cs (Development uniquement).
/// </summary>
public sealed class DbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DbInitializerHostedService> _logger;

    public DbInitializerHostedService(IServiceProvider services, ILogger<DbInitializerHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        try
        {
            await initializer.InitializeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
