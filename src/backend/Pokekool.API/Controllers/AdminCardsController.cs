using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.Application.Collection.Commands.AddScanRunToStock;
using Pokekool.Application.Collection.Commands.ReindexCardEmbeddings;
using Pokekool.Application.Collection.Commands.RerunScanBatchItems;
using Pokekool.Application.Collection.Commands.RunScanBatch;
using Pokekool.Application.Collection.Commands.ScanVisionModelSettings;
using Pokekool.Application.Collection.Commands.StartScanRun;
using Pokekool.Application.Collection.Queries.GetScanConfidenceStats;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Domain.Repositories;
using Pokekool.Infrastructure.Services;
using Pokekool.Shared.Dtos.Collection;
using Pokekool.Shared.Dtos.Sync;

namespace Pokekool.API.Controllers;

/// <summary>
/// Endpoints Admin pour le catalogue de cartes — indexation des embeddings visuels (DINOv2)
/// utilisés par le matching par similarité d'image, en comparatif avec le matching texte LLM
/// vision existant. Voir 03-Metier/Automatisations-LLM.md § Matching visuel (DINOv2).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/cards")]
[Produces("application/json")]
[Authorize]
public sealed class AdminCardsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly EmbeddingReindexState _reindexState;
    private readonly IScanBatchFileStore _scanBatchFileStore;
    private readonly IScanRunRepository _scanRunRepo;

    public AdminCardsController(
        IMediator mediator,
        EmbeddingReindexState reindexState,
        IScanBatchFileStore scanBatchFileStore,
        IScanRunRepository scanRunRepo)
    {
        _mediator = mediator;
        _reindexState = reindexState;
        _scanBatchFileStore = scanBatchFileStore;
        _scanRunRepo = scanRunRepo;
    }

    /// <summary>
    /// Indexe un lot de cartes sans embedding : télécharge l'image officielle (ImageLarge ou
    /// ImageSmall), calcule le vecteur DINOv2, le stocke. Conçu pour rester appelable en
    /// synchrone sans timeout HTTP — rappeler avec le même batchSize jusqu'à Remaining == 0
    /// pour indexer tout le catalogue.
    /// </summary>
    [HttpPost("reindex-embeddings")]
    public async Task<ActionResult<ReindexCardEmbeddingsResult>> ReindexEmbeddings(
        [FromQuery] int batchSize = 200, CancellationToken ct = default)
        => Ok(await _mediator.Send(new ReindexCardEmbeddingsCommand(batchSize), ct));

    /// <summary>
    /// État de la réindexation automatique des embeddings au démarrage de l'API
    /// (voir EmbeddingReindexHostedService) — consommé par l'overlay de progression topbar
    /// pour savoir si le scan visuel peut être utilisé "en toute confiance".
    /// </summary>
    [HttpGet("reindex-embeddings/status")]
    [AllowAnonymous]
    public ActionResult<EmbeddingReindexStatusDto> GetReindexStatus()
        => Ok(new EmbeddingReindexStatusDto(
            Status: _reindexState.Status.ToString().ToLowerInvariant(),
            TotalItems: _reindexState.TotalItems,
            ItemsProcessed: _reindexState.ItemsProcessed,
            ItemsFailed: _reindexState.ItemsFailed,
            Remaining: _reindexState.Remaining,
            StartedAt: _reindexState.StartedAt,
            CompletedAt: _reindexState.CompletedAt));

    /// <summary>
    /// Étape 1 du pipeline "Analyse du dossier" — ouvre un ScanRun persisté avec sa destination
    /// (boutique ou collection tierce) AVANT de lancer l'analyse. Voir StartScanRunCommand pour
    /// les règles de validation (nom + motif requis si Tiers, etc.).
    /// </summary>
    [HttpPost("scan-batch/start")]
    public async Task<ActionResult<Guid>> StartScanRun(
        [FromBody] StartScanRunRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new StartScanRunCommand(
            Pokekool.Application.Collection.ScanEnumMap.ParseDestinationType(request.DestinationType),
            request.AddedToStockDirectly,
            request.ThirdPartyName,
            Pokekool.Application.Collection.ScanEnumMap.ParseReasonOrNull(request.Reason),
            request.ReasonOtherText), ct));

    /// <summary>
    /// Analyse en DINOv2 pur toutes les photos actuellement dans scan-batch/_in, écrit un
    /// rapport (scan-batch/_out/_report.csv) puis y déplace les photos, et persiste le résultat
    /// sur le ScanRun ouvert par StartScanRun. Synchrone — peut être long sur un gros volume.
    /// </summary>
    [HttpPost("scan-batch/{scanRunId:guid}/run")]
    public async Task<ActionResult<ScanBatchResult>> RunScanBatch(Guid scanRunId, CancellationToken ct)
        => Ok(await _mediator.Send(new RunScanBatchCommand(scanRunId), ct));

    /// <summary>
    /// Reçoit les photos d'un dossier sélectionné côté web (input webkitdirectory) et les écrit
    /// dans scan-batch/_in, prêtes pour "Lancer l'analyse". Rejette silencieusement les fichiers
    /// non-image (le sélecteur de dossier peut remonter des .DS_Store, Thumbs.db, etc.).
    /// </summary>
    [HttpPost("scan-batch/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(500_000_000)]
    public async Task<ActionResult<int>> UploadToScanBatch(List<IFormFile> files, CancellationToken ct)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var saved = 0;

        foreach (var file in files)
        {
            if (file.Length == 0)
                continue;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!imageExtensions.Contains(ext))
                continue;

            await using var stream = file.OpenReadStream();
            await _scanBatchFileStore.SavePendingPhotoAsync(file.FileName, stream, ct);
            saved++;
        }

        return Ok(saved);
    }

    /// <summary>Historique des runs (scans de dossier), persisté en base, du plus récent au plus
    /// ancien — traçabilité des collections boutique et tierces (rachat, estimation...).</summary>
    [HttpGet("scan-batch/history")]
    public async Task<ActionResult<IReadOnlyList<ScanBatchHistoryEntryDto>>> GetScanBatchHistory(CancellationToken ct)
    {
        var runs = await _scanRunRepo.ListSummariesAsync(ct);
        return Ok(runs.Select(ToHistoryEntryDto).ToList());
    }

    /// <summary>
    /// Détail complet d'un run (une ligne par carte analysée, avec le nom de la photo archivée) —
    /// alimente la modal de détail de l'historique côté web.
    /// </summary>
    [HttpGet("scan-batch/history/{scanRunId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ScanBatchItemDto>>> GetScanBatchRunDetail(
        Guid scanRunId, CancellationToken ct)
    {
        var run = await _scanRunRepo.GetWithItemsAsync(scanRunId, ct);
        if (run is null)
            return NotFound();

        return Ok(run.Items.Select(ToItemDto).ToList());
    }

    /// <summary>Sert la photo archivée d'un run (celle réellement scannée) pour la modal de détail.</summary>
    [HttpGet("scan-batch/history/{scanRunId:guid}/photos/{fileName}")]
    [AllowAnonymous] // consommée comme <img src>, pas d'appel XHR authentifié possible côté navigateur
    public ActionResult GetArchivedPhoto(Guid scanRunId, string fileName)
    {
        var path = _scanBatchFileStore.GetArchivedPhotoPath(scanRunId.ToString(), fileName);
        if (path is null)
            return NotFound();

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            _ => "image/jpeg",
        };
        return PhysicalFile(path, contentType);
    }

    /// <summary>
    /// Ré-analyse ciblée : relance DINOv2 sur les photos cochées comme "refusées" dans la modal
    /// de détail, en excluant la carte précédemment proposée pour chacune. Met à jour le run en
    /// place — un rafraîchissement de la modal montrera les nouveaux candidats.
    /// </summary>
    [HttpPost("scan-batch/history/{scanRunId:guid}/rerun")]
    public async Task<ActionResult<IReadOnlyList<ScanBatchItemDto>>> RerunScanBatchItems(
        Guid scanRunId, [FromBody] List<RerunScanBatchItemRequest> items, CancellationToken ct)
        => Ok(await _mediator.Send(new RerunScanBatchItemsCommand(scanRunId, items), ct));

    /// <summary>
    /// Étape explicite "🏪 Ajouter au stock" du récap (bouton à côté du "✕") — pour un run
    /// Boutique dont l'ajout n'a pas été fait automatiquement (AddedToStockDirectly = false/null).
    /// Idempotent : sans effet si déjà commité (CreatedBinderId déjà renseigné) — voir
    /// ScanRunStockCommitter. À appeler UNE FOIS que les corrections (refuser/relancer) sont
    /// terminées, pour éviter tout dédoublement du stock.
    /// </summary>
    [HttpPost("scan-batch/history/{scanRunId:guid}/add-to-stock")]
    public async Task<ActionResult<AddScanRunToStockResult>> AddScanRunToStock(
        Guid scanRunId, CancellationToken ct)
        => Ok(await _mediator.Send(new AddScanRunToStockCommand(scanRunId), ct));

    /// <summary>
    /// Modèle Ollama Vision actuellement utilisé par le fallback texte/vision du scan (3e tentative
    /// et au-delà — voir RerunScanBatchItemsCommandHandler) et par le scan mobile "1 carte à la
    /// fois" (ScanCollectionCommandHandler). Paramètres › Scan (IA Vision), comparatif CPU
    /// llava:7b / llava-phi3 / moondream demandé le 2026-09-11.
    /// </summary>
    [HttpGet("scan-batch/vision-model")]
    public async Task<ActionResult<ScanVisionModelSettingsDto>> GetScanVisionModel(CancellationToken ct)
        => Ok(await _mediator.Send(new GetScanVisionModelSettingsQuery(), ct));

    /// <summary>Change le modèle Ollama Vision (voir GetScanVisionModel) — appliqué au prochain
    /// appel du fallback texte/vision, sans redémarrage de l'API.</summary>
    [HttpPut("scan-batch/vision-model")]
    public async Task<ActionResult<ScanVisionModelSettingsDto>> UpdateScanVisionModel(
        [FromBody] UpdateScanVisionModelRequest body, CancellationToken ct)
    {
        await _mediator.Send(new UpdateScanVisionModelSettingsCommand(body.Model), ct);
        return Ok(await _mediator.Send(new GetScanVisionModelSettingsQuery(), ct));
    }

    /// <summary>
    /// Indice de confiance agrégé sur TOUT l'historique persisté des scans (et non plus seulement
    /// le dernier run) — widget "Indice de confiance" du récap. Voir ScanConfidenceStatsDto pour
    /// la définition exacte des trois catégories. Corrigé le 2026-09-11, demande explicite de Gab.
    /// </summary>
    [HttpGet("scan-batch/confidence-stats")]
    public async Task<ActionResult<ScanConfidenceStatsDto>> GetScanConfidenceStats(CancellationToken ct)
        => Ok(await _mediator.Send(new GetScanConfidenceStatsQuery(), ct));

    private static ScanBatchHistoryEntryDto ToHistoryEntryDto(Pokekool.Domain.Entities.Collection.ScanRun run)
        => new(
            run.Id, run.RunAt, run.DestinationType.ToString(), run.AddedToStockDirectly,
            run.ThirdPartyName, run.Reason?.ToString(), run.ReasonOtherText,
            run.TotalPhotos, run.Succeeded, run.Failed, run.AverageDistance, run.AverageElapsedMs,
            run.CreatedBinderId);

    private static ScanBatchItemDto ToItemDto(Pokekool.Domain.Entities.Collection.ScanRunItem item)
        => new(
            item.FileName, item.ArchivedPhotoFileName, item.Success, item.BestCardId,
            item.BestCardName, item.BestSetName, item.BestSetReleaseYear, item.BestCardNumber,
            item.BestImageSmall, item.BestDistance, item.ElapsedMs, item.Error,
            item.EstimatedPrice, item.EstimatedPriceCurrency);
}
