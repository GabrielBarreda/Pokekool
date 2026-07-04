using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Application.Pokemon.Commands.SyncCardPopulation;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Services;

namespace Pokekool.Infrastructure.BackgroundServices;

/// <summary>
/// BackgroundService qui exécute le sync prix global à l'heure planifiée.
/// Supporte one-shot et récurrent (quotidien).
/// </summary>
public sealed class ScheduledSyncService : BackgroundService
{
    private readonly IServiceProvider   _services;
    private readonly SyncScheduleState  _state;
    private readonly ILogger<ScheduledSyncService> _logger;

    public ScheduledSyncService(
        IServiceProvider services,
        SyncScheduleState state,
        ILogger<ScheduledSyncService> logger)
    {
        _services = services;
        _state    = state;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledSyncService démarré.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_state.NextRunAt.HasValue
                && DateTime.UtcNow >= _state.NextRunAt.Value
                && _state.Status   == SyncScheduleStatus.Pending)
            {
                await RunSyncAsync(stoppingToken);
            }

            // Vérifie toutes les 30 secondes
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        _state.Status = SyncScheduleStatus.Running;
        _logger.LogInformation("ScheduledSyncService — sync planifié démarré à {UtcNow:u}", DateTime.UtcNow);

        try
        {
            using var scope = _services.CreateScope();
            var aggregator = scope.ServiceProvider.GetRequiredService<DataAggregator>();
            var cardRepo   = scope.ServiceProvider.GetRequiredService<IPokemonCardRepository>();

            var allCards = await cardRepo.GetAllAsync(ct);
            var cardIds  = allCards.Select(c => c.Id);

            var result = await aggregator.SyncPricesAsync(cardIds, ct);

            _state.LastCompletedAt    = DateTime.UtcNow;
            _state.LastPricesUpserted = result.PricesUpserted;

            _logger.LogInformation(
                "ScheduledSyncService — prix terminés : {Upserted} prix, {Failed} échec(s).",
                result.PricesUpserted, result.CardsFailed);

            // ── Sync population (après les prix) ──────────────────────────────
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var popResult = await mediator.Send(new SyncCardPopulationCommand(null), ct);

            _logger.LogInformation(
                "ScheduledSyncService — population terminée : {Upserted} upserts, {Failed} échec(s).",
                popResult.PopulationsUpserted, popResult.CardsFailed);

            _logger.LogInformation("ScheduledSyncService — sync complet.");

            if (_state.IsRecurring)
            {
                _state.NextRunAt = _state.NextRunAt!.Value.AddDays(1);
                _state.Status    = SyncScheduleStatus.Pending;
                _logger.LogInformation("ScheduledSyncService — prochain run : {Next:u}", _state.NextRunAt);
            }
            else
            {
                _state.Status = SyncScheduleStatus.Completed;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _state.Status = SyncScheduleStatus.Cancelled;
            _logger.LogWarning("ScheduledSyncService — annulé (arrêt du serveur).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScheduledSyncService — erreur inattendue.");
            _state.Status = SyncScheduleStatus.Completed; // évite une boucle infinie
        }
    }
}
