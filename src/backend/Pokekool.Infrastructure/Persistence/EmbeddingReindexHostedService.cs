using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Collection.Commands.ReindexCardEmbeddings;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Services;

namespace Pokekool.Infrastructure.Persistence;

/// <summary>
/// Au démarrage de l'API, vérifie s'il reste des cartes du catalogue sans embedding visuel
/// (DINOv2, voir 03-Metier/Automatisations-LLM.md § Matching visuel) et lance la réindexation
/// automatiquement si oui — sans action manuelle requise (plus besoin d'appeler l'endpoint
/// reindex-embeddings à la main après chaque redémarrage/rebuild).
///
/// Tourne en tâche de fond (StartAsync ne l'attend pas) : un catalogue de plusieurs milliers de
/// cartes prendrait sinon plusieurs minutes à indexer et retarderait d'autant le passage de l'API
/// en "healthy" — inutile de bloquer le démarrage pour ça, le scan fonctionne dès que l'API est
/// up (juste sans comparatif visuel tant que l'indexation n'est pas terminée).
///
/// Progression (2026-09-07) : publie l'avancement dans <see cref="EmbeddingReindexState"/> (lu
/// par l'endpoint GET /api/v1/admin/cards/reindex-embeddings/status) et diffuse un
/// SyncProgressEvent par lot traité via ISyncProgressReporter (SyncType "embeddings"), pour que
/// l'overlay topbar affiche l'avancement en direct au même titre que les 3 autres syncs.
///
/// Fix boucle infinie (2026-09-07) : ReindexCardEmbeddingsCommandHandler.GetWithoutEmbeddingAsync
/// resélectionne systématiquement les mêmes cartes tant qu'elles n'ont pas d'embedding stocké en
/// base — or une carte dont l'image source est cassée (404 côté images.pokemontcg.io, cas réel
/// observé sur le set mcd15) échouera À CHAQUE tentative, indéfiniment : elle ne sort jamais de
/// "sans embedding". L'ancienne condition d'arrêt (Processed == 0 && Failed == 0) ne couvrait pas
/// ce cas puisque Failed > 0 à chaque lot — la boucle tournait alors sans fin sur le même lot de
/// cartes en échec permanent (observé : 9048 échecs cumulés pour 29 cartes réellement concernées).
/// Fix : plafond MaxBatches, et arrêt dès qu'un lot n'a traité aucune carte avec succès (Processed
/// == 0), qu'il y ait des échecs ou non — un lot 100% en échec ne progressera jamais en réessayant.
/// </summary>
public sealed class EmbeddingReindexHostedService : IHostedService
{
    private const int BatchSize = 200;

    // Sécurité anti-boucle-infinie : un catalogue de plusieurs milliers de cartes tient largement
    // dans cette limite (ex: 9048 cartes / 200 par lot ≈ 46 lots) ; au-delà, quelque chose ne
    // progresse plus normalement et il vaut mieux s'arrêter que de tourner indéfiniment.
    private const int MaxBatches = 200;

    private readonly IServiceProvider _services;
    private readonly EmbeddingReindexState _state;
    private readonly ISyncProgressReporter _progress;
    private readonly ILogger<EmbeddingReindexHostedService> _logger;

    public EmbeddingReindexHostedService(
        IServiceProvider services,
        EmbeddingReindexState state,
        ISyncProgressReporter progress,
        ILogger<EmbeddingReindexHostedService> logger)
    {
        _services = services;
        _state = state;
        _progress = progress;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = RunAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var cardRepo = scope.ServiceProvider.GetRequiredService<IPokemonCardRepository>();

            var missing = await cardRepo.CountWithoutEmbeddingAsync(ct);
            if (missing == 0)
            {
                _logger.LogInformation("[Réindexation DINOv2] catalogue déjà à jour, rien à faire.");
                _state.Status = EmbeddingReindexStatus.Completed;
                _state.TotalItems = 0;
                _state.ItemsProcessed = 0;
                _state.ItemsFailed = 0;
                _state.CompletedAt = DateTime.UtcNow;
                return;
            }

            _logger.LogInformation(
                "[Réindexation DINOv2] {Missing} carte(s) sans embedding détectée(s) au démarrage — indexation en tâche de fond.",
                missing);

            _state.Status = EmbeddingReindexStatus.Running;
            _state.TotalItems = missing;
            _state.ItemsProcessed = 0;
            _state.ItemsFailed = 0;
            _state.StartedAt = DateTime.UtcNow;
            _state.CompletedAt = null;

            _progress.Report(new SyncProgressEvent(
                SetName: "", CardName: $"Démarrage — {missing} carte(s) à indexer", CardNumber: "",
                Grades: [], Success: true, Timestamp: DateTime.UtcNow,
                SyncType: "embeddings", TotalItems: missing));

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var batchCount = 0;
            int remaining;
            do
            {
                batchCount++;
                var result = await mediator.Send(new ReindexCardEmbeddingsCommand(BatchSize), ct);
                remaining = result.Remaining;

                _state.ItemsProcessed += result.Processed;
                _state.ItemsFailed += result.Failed;

                _logger.LogInformation(
                    "[Réindexation DINOv2] lot {BatchCount} traité : {Processed} ok, {Failed} échecs, {Remaining} restantes.",
                    batchCount, result.Processed, result.Failed, remaining);

                _progress.Report(new SyncProgressEvent(
                    SetName: "",
                    CardName: $"{_state.ItemsProcessed + _state.ItemsFailed}/{_state.TotalItems} traitées ({result.Failed} échec(s) sur ce lot)",
                    CardNumber: "",
                    Grades: [], Success: result.Failed == 0, Timestamp: DateTime.UtcNow,
                    SyncType: "embeddings", TotalItems: _state.TotalItems));

                // Un lot n'ayant traité AUCUNE carte avec succès (Processed == 0) ne progressera
                // jamais en réessayant, même s'il contient des échecs (image cassée en base
                // fournisseur — pas un problème réseau transitoire) : on arrête plutôt que de
                // reboucler indéfiniment sur les mêmes cartes.
                if (result.Processed == 0)
                {
                    if (result.Failed > 0)
                    {
                        _logger.LogWarning(
                            "[Réindexation DINOv2] arrêt anticipé : {Failed} carte(s) en échec permanent " +
                            "(image source introuvable), {Remaining} carte(s) resteront sans embedding.",
                            result.Failed, remaining);
                    }
                    break;
                }
            }
            while (remaining > 0 && batchCount < MaxBatches && !ct.IsCancellationRequested);

            if (batchCount >= MaxBatches && remaining > 0)
            {
                _logger.LogWarning(
                    "[Réindexation DINOv2] arrêt sur plafond de sécurité ({MaxBatches} lots) — {Remaining} carte(s) restantes, " +
                    "sera retenté au prochain démarrage de l'API.",
                    MaxBatches, remaining);
            }

            _state.Status = EmbeddingReindexStatus.Completed;
            _state.CompletedAt = DateTime.UtcNow;

            _progress.Report(new SyncProgressEvent(
                SetName: "", CardName: $"Terminé — {_state.ItemsProcessed} ok, {_state.ItemsFailed} échec(s)", CardNumber: "",
                Grades: [], Success: _state.ItemsFailed == 0, Timestamp: DateTime.UtcNow,
                SyncType: "embeddings", TotalItems: _state.TotalItems));

            _logger.LogInformation(
                "[Réindexation DINOv2] terminée — {Processed} ok, {Failed} échec(s) définitif(s), {Remaining} restante(s) (image manquante ou plafond atteint).",
                _state.ItemsProcessed, _state.ItemsFailed, remaining);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Réindexation DINOv2] erreur pendant la réindexation au démarrage (ignorée, l'API reste utilisable).");
            _state.Status = EmbeddingReindexStatus.Completed;
            _state.CompletedAt = DateTime.UtcNow;
        }
    }
}
