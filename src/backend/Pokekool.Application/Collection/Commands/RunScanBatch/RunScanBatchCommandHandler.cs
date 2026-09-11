using System.Diagnostics;
using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Domain.Entities.Collection;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.RunScanBatch;

public sealed class RunScanBatchCommandHandler : IRequestHandler<RunScanBatchCommand, ScanBatchResult>
{
    private const int VisualMatchTopN = 1;

    private readonly IScanBatchFileStore _fileStore;
    private readonly ICardEmbeddingService _embeddingService;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IScanRunRepository _scanRunRepo;
    private readonly ICardMarketPriceRepository _priceRepo;
    private readonly IScanRunStockCommitter _stockCommitter;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RunScanBatchCommandHandler> _logger;

    public RunScanBatchCommandHandler(
        IScanBatchFileStore fileStore,
        ICardEmbeddingService embeddingService,
        IPokemonCardRepository cardRepo,
        IScanRunRepository scanRunRepo,
        ICardMarketPriceRepository priceRepo,
        IScanRunStockCommitter stockCommitter,
        IUnitOfWork uow,
        ILogger<RunScanBatchCommandHandler> logger)
    {
        _fileStore = fileStore;
        _embeddingService = embeddingService;
        _cardRepo = cardRepo;
        _scanRunRepo = scanRunRepo;
        _priceRepo = priceRepo;
        _stockCommitter = stockCommitter;
        _uow = uow;
        _logger = logger;
    }

    public async Task<ScanBatchResult> Handle(RunScanBatchCommand request, CancellationToken cancellationToken)
    {
        // GetWithItemsAsync (et non GetByIdAsync) : la collection Items doit être explicitement
        // chargée/trackée par EF Core avant AddItem ci-dessous, sinon SaveChangesAsync peut
        // générer un UPDATE incohérent sur une navigation non suivie → DbUpdateConcurrencyException
        // ("0 row(s) affected") observée en test.
        var scanRun = await _scanRunRepo.GetWithItemsAsync(request.ScanRunId, cancellationToken)
            ?? throw Pokekool.Domain.Entities.Collection.ScanErrors.RunNotFound(request.ScanRunId);

        // Sous-dossier d'archive (_history/<ScanRunId>/) — on réutilise l'Id du ScanRun tel quel
        // comme nom de dossier, pour éviter toute dérive entre l'identifiant disque et la clé de
        // vérité côté base (utilisé aussi par GetArchivedPhotoPath dans le contrôleur).
        var archiveId = request.ScanRunId.ToString();

        var photos = _fileStore.ListPendingPhotos();
        var items = new List<ScanBatchItemDto>();

        _logger.LogInformation(
            "[Scan batch] démarrage run {ScanRunId} — {Count} photo(s) dans _in",
            request.ScanRunId, photos.Count);

        foreach (var filePath in photos)
        {
            var fileName = Path.GetFileName(filePath);
            var sw = Stopwatch.StartNew();

            // Archive AVANT tout déplacement — _fileStore.MoveToOut() déplace le fichier source
            // en fin de boucle, la copie doit donc avoir lieu pendant qu'il est encore dans _in.
            var archivedFileName = _fileStore.CopyToRunArchive(filePath, archiveId);

            try
            {
                var base64 = await _fileStore.ReadPhotoAsBase64Async(filePath, cancellationToken);
                var embedding = await _embeddingService.EmbedAsync(base64, cancellationToken);

                if (embedding.Status != CardEmbeddingStatus.Ok || embedding.Embedding is null)
                {
                    sw.Stop();
                    items.Add(new ScanBatchItemDto(
                        fileName, archivedFileName, Success: false, null, null, null, null, null, null, null,
                        sw.ElapsedMilliseconds, $"Embedding indisponible ({embedding.Status}): {embedding.Diagnostic}"));
                    _fileStore.MoveToOut(filePath);
                    continue;
                }

                var candidates = await _cardRepo.SearchByEmbeddingAsync(embedding.Embedding, VisualMatchTopN, ct: cancellationToken);
                sw.Stop();

                if (candidates.Count == 0)
                {
                    items.Add(new ScanBatchItemDto(
                        fileName, archivedFileName, Success: false, null, null, null, null, null, null, null,
                        sw.ElapsedMilliseconds, "Aucun candidat visuel (catalogue non indexé ?)"));
                }
                else
                {
                    var best = candidates[0];
                    items.Add(new ScanBatchItemDto(
                        fileName, archivedFileName, Success: true, best.Card.Id,
                        best.Card.Name, best.Card.Set?.Name, best.Card.Set?.ReleaseDate?.Year,
                        best.Card.Number, best.Card.ImageSmall, best.Distance,
                        sw.ElapsedMilliseconds, Error: null));
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "[Scan batch] erreur sur {FileName}", fileName);
                items.Add(new ScanBatchItemDto(
                    fileName, archivedFileName, Success: false, null, null, null, null, null, null, null,
                    sw.ElapsedMilliseconds, ex.Message));
            }

            _fileStore.MoveToOut(filePath);
        }

        var succeeded = items.Count(i => i.Success);
        var distances = items.Where(i => i.Success && i.BestDistance.HasValue).Select(i => i.BestDistance!.Value).ToList();
        var avgDistance = distances.Count > 0 ? distances.Average() : (double?)null;
        var avgElapsed = items.Count > 0 ? (long)items.Average(i => i.ElapsedMs) : 0L;

        await _fileStore.WriteReportAsync(BuildCsv(items), cancellationToken);

        // Persistance en base (remplace l'ancien historique fichier _history/_index.jsonl) —
        // traçabilité durable, cf. besoin explicite : "les run enregistrés dans l'historique
        // doivent être persistés (pas que du local)".
        scanRun.CompleteWithResults(items.Count, succeeded, items.Count - succeeded, avgDistance, avgElapsed);
        var scanRunItemsByFileName = new Dictionary<string, ScanRunItem>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var scanRunItem = ScanRunItem.Create(
                scanRun.Id, item.FileName, item.ArchivedPhotoFileName, item.Success,
                item.BestCardId, item.BestCardName, item.BestSetName, item.BestSetReleaseYear,
                item.BestCardNumber, item.BestImageSmall, item.BestDistance, item.ElapsedMs, item.Error);
            scanRun.AddItem(scanRunItem);
            // AddItemAsync : attache explicitement l'entité au ChangeTracker EF Core (état
            // Added). AddItem(...) seul (navigation Items) ne suffit pas ici — sur un ScanRun
            // déjà chargé via GetWithItemsAsync, EF Core ne détecte pas fiablement ces nouveaux
            // items comme "Added" et génère un UPDATE au lieu d'un INSERT
            // (DbUpdateConcurrencyException observée en test). Voir IScanRunRepository.
            await _scanRunRepo.AddItemAsync(scanRunItem, cancellationToken);
            scanRunItemsByFileName[item.FileName] = scanRunItem;
        }

        // Cartes reconnues avec succès de ce run, dédupliquées (une même carte physique peut
        // apparaître deux fois si l'employé a repris une photo) — base commune aux deux branches
        // ci-dessous (ajout au stock boutique, ou estimation de prix pour une collection tierce).
        var matchedCardIds = items
            .Where(i => i.Success && i.BestCardId.HasValue)
            .Select(i => i.BestCardId!.Value)
            .Distinct()
            .ToList();

        if (matchedCardIds.Count > 0)
        {
            // Boutique + "Ajouter directement au stock ?" = Oui (popup StartScanRun) : commit
            // immédiat, comportement "rapide" historique, sans étape de vérification. Dans tous
            // les autres cas Boutique (Non / non renseigné), on NE commit PAS ici — l'utilisateur
            // vérifie/refuse/relance depuis le récap, puis clique explicitement "🏪 Ajouter au
            // stock" (AddScanRunToStockCommand). Évite le dédoublement de stock observé quand une
            // correction post-commit n'était jamais répercutée sur les lignes déjà créées.
            if (scanRun.DestinationType == ScanDestinationType.Boutique && scanRun.AddedToStockDirectly == true)
            {
                await _stockCommitter.CommitAsync(scanRun, cancellationToken);
            }
            else if (scanRun.DestinationType != ScanDestinationType.Boutique) // Tiers
            {
                await EstimatePricesAsync(matchedCardIds, scanRunItemsByFileName, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Ré-injecte le prix estimé (Tiers) dans les DTOs retournés — calculé après la
        // construction initiale de `items`, donc pas encore présent dessus.
        var itemsWithEstimatedPrice = items
            .Select(item =>
            {
                var scanRunItem = scanRunItemsByFileName[item.FileName];
                return scanRunItem.EstimatedPrice is null
                    ? item
                    : item with
                    {
                        EstimatedPrice = scanRunItem.EstimatedPrice,
                        EstimatedPriceCurrency = scanRunItem.EstimatedPriceCurrency,
                    };
            })
            .ToList();

        _logger.LogInformation(
            "[Scan batch] terminé run {ScanRunId} — {Succeeded}/{Total} réussi(s), distance moy. {AvgDistance:F4}, {AvgElapsed} ms/photo",
            scanRun.Id, succeeded, items.Count, avgDistance ?? -1, avgElapsed);

        return new ScanBatchResult(
            scanRun.Id, items.Count, succeeded, items.Count - succeeded, avgDistance, avgElapsed, itemsWithEstimatedPrice);
    }

    /// <summary>
    /// Destination Tiers : résout le meilleur prix RAW disponible en catalogue pour chaque carte
    /// reconnue (même règle de priorité de source que GetMarketCardsQueryHandler côté Bourse) et
    /// le stocke sur chaque ScanRunItem correspondant — sert d'estimation rapide pour le
    /// vendeur, sans parler de gradation.
    /// </summary>
    private async Task EstimatePricesAsync(
        IReadOnlyList<Guid> matchedCardIds,
        IReadOnlyDictionary<string, ScanRunItem> scanRunItemsByFileName,
        CancellationToken ct)
    {
        var pricesByCard = await _priceRepo.GetByCardIdsAsync(matchedCardIds, ct);

        foreach (var scanRunItem in scanRunItemsByFileName.Values)
        {
            if (scanRunItem.BestCardId is not { } cardId)
                continue;

            pricesByCard.TryGetValue(cardId, out var prices);
            var (market, _, currency, _) = ResolveBestPrice(prices);
            scanRunItem.SetEstimatedPrice(market, currency);
        }
    }

    /// <summary>Priorité : tcgplayer holofoil > tcgplayer normal > cardmarket — reprend la même
    /// règle que GetMarketCardsQueryHandler (Bourse) pour rester cohérent avec le prix affiché
    /// ailleurs dans l'app. Retourne (marketPrice, lowPrice, currency, source).</summary>
    private static (decimal? market, decimal? low, string? currency, string? source) ResolveBestPrice(
        IReadOnlyList<CardMarketPrice>? prices)
    {
        if (prices is null || prices.Count == 0)
            return (null, null, null, null);

        var tcgHolo = prices.FirstOrDefault(p => p.Source == "tcgplayer" && p.Variant == "holofoil");
        if (tcgHolo?.MarketPrice is not null)
            return (tcgHolo.MarketPrice, tcgHolo.LowPrice, tcgHolo.Currency, "tcgplayer/holofoil");

        var tcgNormal = prices.FirstOrDefault(p => p.Source == "tcgplayer" && p.Variant == "normal");
        if (tcgNormal?.MarketPrice is not null)
            return (tcgNormal.MarketPrice, tcgNormal.LowPrice, tcgNormal.Currency, "tcgplayer/normal");

        var cm = prices.FirstOrDefault(p => p.Source == "cardmarket");
        if (cm?.MarketPrice is not null)
            return (cm.MarketPrice, cm.LowPrice, cm.Currency, "cardmarket");

        return (null, null, null, null);
    }

    private static string BuildCsv(IReadOnlyList<ScanBatchItemDto> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FileName;Success;BestCardName;BestSetName;BestSetReleaseYear;BestCardNumber;BestDistance;ElapsedMs;Error");
        foreach (var i in items)
        {
            sb.AppendLine(string.Join(';', new[]
            {
                Csv(i.FileName),
                i.Success.ToString(),
                Csv(i.BestCardName),
                Csv(i.BestSetName),
                i.BestSetReleaseYear?.ToString(CultureInfo.InvariantCulture) ?? "",
                Csv(i.BestCardNumber),
                i.BestDistance?.ToString("F4", CultureInfo.InvariantCulture) ?? "",
                i.ElapsedMs.ToString(CultureInfo.InvariantCulture),
                Csv(i.Error),
            }));
        }
        return sb.ToString();

        static string Csv(string? value) =>
            value is null ? "" : "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
