using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Storage;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Infrastructure.Storage;

namespace Pokekool.Infrastructure.BackgroundServices;

/// <summary>
/// Consomme la file des jobs de compilation Blender (Channel) et compile chaque .blend en .glb.
/// Suit le pattern <c>ScheduledSyncService</c> (BackgroundService custom, scope par job).
/// Aucune dépendance à Hangfire (non installé dans ce repo).
/// </summary>
internal sealed class BlenderCompileBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly InMemoryCompileQueue _queue;
    private readonly ILogger<BlenderCompileBackgroundService> _logger;

    public BlenderCompileBackgroundService(
        IServiceProvider services,
        InMemoryCompileQueue queue,
        ILogger<BlenderCompileBackgroundService> logger)
    {
        _services = services;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlenderCompileBackgroundService démarré.");

        await foreach (var jobId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlenderCompileBackgroundService — erreur inattendue sur le job {JobId}.", jobId);
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var jobRepo = sp.GetRequiredService<IStorageModelCompileJobRepository>();
        var roomRepo = sp.GetRequiredService<IStorageRoomRepository>();
        var shopRepo = sp.GetRequiredService<IShopRepository>();
        var compiler = sp.GetRequiredService<IBlenderCompiler>();
        var fileStore = sp.GetRequiredService<IBlenderFileStore>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var job = await jobRepo.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            _logger.LogWarning("BlenderCompileBackgroundService — job {JobId} introuvable, ignoré.", jobId);
            return;
        }

        job.MarkRunning();
        await uow.SaveChangesAsync(ct);
        _logger.LogInformation("Compilation Blender démarrée pour le job {JobId} ({Blend}).", job.Id, job.BlendFilePath);

        var outputPath = fileStore.GetGlbOutputPath(job.Id);
        var result = await compiler.CompileAsync(job.BlendFilePath, outputPath, ct);

        if (result.IsSuccess && result.OutputGlbPath is not null)
        {
            var publicUrl = fileStore.ToPublicUrl(result.OutputGlbPath);
            job.MarkSucceeded(publicUrl);

            if (job.TargetType == StorageModelCompileJob.TargetRoom)
            {
                var room = await roomRepo.GetByIdAsync(job.TargetId, ct);
                room?.SetModel3dUrl(publicUrl);
            }
            else if (job.TargetType == StorageModelCompileJob.TargetShop)
            {
                var shop = await shopRepo.GetByIdAsync(job.TargetId, ct);
                shop?.SetPlvModel3dUrl(publicUrl);
            }

            await uow.SaveChangesAsync(ct);
            _logger.LogInformation("Compilation Blender réussie pour le job {JobId} → {Url}.", job.Id, publicUrl);
        }
        else
        {
            job.MarkFailed(result.Error ?? "Échec inconnu.");
            await uow.SaveChangesAsync(ct);
            _logger.LogError("Compilation Blender échouée pour le job {JobId} : {Err}.", job.Id, result.Error);
        }
    }
}
