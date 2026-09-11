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

namespace Pokekool.Application.Collection.Commands.RerunScanBatchItems;

public sealed class RerunScanBatchItemsCommandHandler
    : IRequestHandler<RerunScanBatchItemsCommand, IReadOnlyList<ScanBatchItemDto>>
{
    private const int VisualMatchTopN = 1;

    // Fallback texte/vision : à partir de la 3e tentative sur une même photo (2 refus déjà
    // cumulés dans ExcludedCardIds), on bascule du matching visuel pur (DINOv2, qui boucle sur
    // les mêmes candidats proches) vers une lecture OCR/LLM Vision (Ollama llava:7b) + matching
    // par nom traduit FR — voir TryVisionFallbackAsync.
    private const int VisionFallbackThreshold = 2;
    private const double VisionAutoMatchConfidence = 0.65;
    private const int TextCandidatePageSize = 20;
    private const int TranslationCandidateLimit = 10;
    private const int EmbeddingArbitrationTopN = 1;

    private readonly IScanBatchFileStore _fileStore;
    private readonly ICardEmbeddingService _embeddingService;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly IScanRunRepository _scanRunRepo;
    private readonly ICardMarketPriceRepository _priceRepo;
    private readonly ICardVisionAnalyzer _visionAnalyzer;
    private readonly IVaultKnowledgeService _vault;
    private readonly ICardTranslationRepository _translationRepo;
    private readonly IAppSettingRepository _appSettings;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RerunScanBatchItemsCommandHandler> _logger;

    public RerunScanBatchItemsCommandHandler(
        IScanBatchFileStore fileStore,
        ICardEmbeddingService embeddingService,
        IPokemonCardRepository cardRepo,
        IScanRunRepository scanRunRepo,
        ICardMarketPriceRepository priceRepo,
        ICardVisionAnalyzer visionAnalyzer,
        IVaultKnowledgeService vault,
        ICardTranslationRepository translationRepo,
        IAppSettingRepository appSettings,
        IUnitOfWork uow,
        ILogger<RerunScanBatchItemsCommandHandler> logger)
    {
        _fileStore = fileStore;
        _embeddingService = embeddingService;
        _cardRepo = cardRepo;
        _scanRunRepo = scanRunRepo;
        _priceRepo = priceRepo;
        _visionAnalyzer = visionAnalyzer;
        _vault = vault;
        _translationRepo = translationRepo;
        _appSettings = appSettings;
        _uow = uow;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScanBatchItemDto>> Handle(
        RerunScanBatchItemsCommand request, CancellationToken cancellationToken)
    {
        var scanRun = await _scanRunRepo.GetWithItemsAsync(request.ScanRunId, cancellationToken)
            ?? throw Pokekool.Domain.Entities.Collection.ScanErrors.RunNotFound(request.ScanRunId);

        var itemsByFileName = scanRun.Items.ToDictionary(i => i.FileName, StringComparer.Ordinal);
        var archiveId = request.ScanRunId.ToString();
        var results = new List<ScanBatchItemDto>();

        foreach (var refusal in request.Items)
        {
            // Cumule la carte refusée à l'historique persisté de l'item AVANT la recherche, pour
            // que celle-ci exclue aussi toutes les cartes déjà refusées lors des relances
            // précédentes — sinon, sur 2 relances successives, on peut reproposer une carte déjà
            // rejetée (ex: Gloom refusé → Erika's Jigglypuff refusé → Gloom reproposé). Si l'item
            // est introuvable (run corrompu), on se rabat sur l'exclusion simple envoyée par le
            // front (comportement précédent).
            IReadOnlyList<Guid> excludedCardIds = new[] { refusal.ExcludedCardId };
            if (itemsByFileName.TryGetValue(refusal.FileName, out var itemForExclusion))
            {
                itemForExclusion.AddExcludedCardId(refusal.ExcludedCardId);
                excludedCardIds = itemForExclusion.ExcludedCardIds.ToList();
            }

            var archivedPath = _fileStore.GetArchivedPhotoPath(archiveId, refusal.ArchivedPhotoFileName);
            var sw = Stopwatch.StartNew();

            ScanBatchItemDto result;

            if (archivedPath is null)
            {
                result = new ScanBatchItemDto(
                    refusal.FileName, refusal.ArchivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                    0, "Photo archivée introuvable (run supprimé/déplacé ?)");
            }
            else
            {
                try
                {
                    var base64 = await _fileStore.ReadPhotoAsBase64Async(archivedPath, cancellationToken);

                    if (excludedCardIds.Count >= VisionFallbackThreshold)
                    {
                        // 3e tentative sur cette photo (2 refus déjà cumulés) : le matching visuel
                        // pur (DINOv2) reboucle sur les mêmes candidats proches sans jamais
                        // retenir le bon — on bascule sur la lecture du texte imprimé (OCR/LLM
                        // Vision) + matching par nom, voir TryVisionFallbackAsync.
                        result = await TryVisionFallbackAsync(
                            refusal.FileName, refusal.ArchivedPhotoFileName, base64, excludedCardIds, sw, cancellationToken);
                    }
                    else
                    {
                        var embedding = await _embeddingService.EmbedAsync(base64, cancellationToken);

                        if (embedding.Status != CardEmbeddingStatus.Ok || embedding.Embedding is null)
                        {
                            sw.Stop();
                            result = new ScanBatchItemDto(
                                refusal.FileName, refusal.ArchivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                                sw.ElapsedMilliseconds, $"Embedding indisponible ({embedding.Status}): {embedding.Diagnostic}");
                        }
                        else
                        {
                            var candidates = await _cardRepo.SearchByEmbeddingAsync(
                                embedding.Embedding, VisualMatchTopN, excludedCardIds: excludedCardIds, cancellationToken);
                            sw.Stop();

                            if (candidates.Count == 0)
                            {
                                result = new ScanBatchItemDto(
                                    refusal.FileName, refusal.ArchivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                                    sw.ElapsedMilliseconds, "Aucun autre candidat visuel (hors carte exclue)");
                            }
                            else
                            {
                                var best = candidates[0];
                                result = new ScanBatchItemDto(
                                    refusal.FileName, refusal.ArchivedPhotoFileName, Success: true, best.Card.Id,
                                    best.Card.Name, best.Card.Set?.Name, best.Card.Set?.ReleaseDate?.Year,
                                    best.Card.Number, best.Card.ImageSmall, best.Distance,
                                    sw.ElapsedMilliseconds, Error: null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogWarning(ex, "[Scan batch] erreur ré-analyse sur {FileName}", refusal.FileName);
                    result = new ScanBatchItemDto(
                        refusal.FileName, refusal.ArchivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                        sw.ElapsedMilliseconds, ex.Message);
                }
            }

            results.Add(result);

            if (itemsByFileName.TryGetValue(refusal.FileName, out var existingItem))
            {
                existingItem.ReplaceResult(
                    result.Success, result.BestCardId, result.BestCardName, result.BestSetName,
                    result.BestSetReleaseYear, result.BestCardNumber, result.BestImageSmall,
                    result.BestDistance, result.ElapsedMs, result.Error);

                // Collection Tiers : la carte proposée a changé, le prix estimé précédent ne
                // correspond plus — le recalcule (ou l'efface si plus aucune carte identifiée).
                if (scanRun.DestinationType == ScanDestinationType.Tiers)
                {
                    if (existingItem.BestCardId is { } newCardId)
                    {
                        var prices = await _priceRepo.GetByCardIdAsync(newCardId, cancellationToken);
                        var (market, _, currency, _) = ResolveBestPrice(prices);
                        existingItem.SetEstimatedPrice(market, currency);
                    }
                    else
                    {
                        existingItem.SetEstimatedPrice(null, null);
                    }
                }
            }
            else
            {
                _logger.LogWarning(
                    "[Scan batch] ré-analyse : aucun ScanRunItem existant pour {FileName} dans le run {ScanRunId}, résultat non persisté.",
                    refusal.FileName, request.ScanRunId);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Ré-injecte le prix estimé recalculé (Tiers) dans les DTOs retournés à l'appelant.
        if (scanRun.DestinationType == ScanDestinationType.Tiers)
        {
            results = results
                .Select(r =>
                {
                    if (!itemsByFileName.TryGetValue(r.FileName, out var scanRunItem))
                        return r;
                    return scanRunItem.EstimatedPrice is null
                        ? r
                        : r with
                        {
                            EstimatedPrice = scanRunItem.EstimatedPrice,
                            EstimatedPriceCurrency = scanRunItem.EstimatedPriceCurrency,
                        };
                })
                .ToList();
        }

        _logger.LogInformation(
            "[Scan batch] ré-analyse run {ScanRunId} — {Count} photo(s) refusée(s) re-testées",
            request.ScanRunId, results.Count);

        return results;
    }

    /// <summary>Priorité : tcgplayer holofoil > tcgplayer normal > cardmarket — reprend la même
    /// règle que GetMarketCardsQueryHandler / RunScanBatchCommandHandler.</summary>
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

    /// <summary>
    /// Fallback texte/vision — déclenché à partir de la 3e tentative sur une même photo (voir
    /// VisionFallbackThreshold) : le matching visuel pur (DINOv2) tourne en boucle sur les mêmes
    /// candidats proches quand aucun n'est le bon, donc on bascule sur une lecture du texte
    /// imprimé par un LLM Vision (Ollama llava:7b — voir ICardVisionAnalyzer ; pas de modèle
    /// payant type GPT/Claude ici, conformément à la contrainte posée) puis un matching par nom
    /// (traduction FR en priorité — désormais disponible, voir ICardTranslationRepository — puis
    /// nom EN en repli) et numéro imprimé, en excluant explicitement toutes les cartes déjà
    /// refusées pour cette photo. Dégradation gracieuse : toute erreur ou échec de matching
    /// retourne un ScanBatchItemDto en échec plutôt que de lever, au même titre que le chemin
    /// DINOv2 existant.
    /// </summary>
    private async Task<ScanBatchItemDto> TryVisionFallbackAsync(
        string fileName,
        string archivedPhotoFileName,
        string imageBase64,
        IReadOnlyList<Guid> excludedCardIds,
        Stopwatch sw,
        CancellationToken ct)
    {
        try
        {
            // Les cartes déjà refusées sont passées en exclusion textuelle au modèle vision, en
            // plus de l'exclusion appliquée après coup sur les candidats du matching par nom.
            var exclusionNames = new List<string>();
            foreach (var excludedId in excludedCardIds)
            {
                var excludedCard = await _cardRepo.GetByIdAsync(excludedId, ct);
                if (excludedCard is not null)
                    exclusionNames.Add(excludedCard.Name);
            }

            var ragContext = await _vault.BuildPromptContextAsync(ct);
            var visionModel = await VisionModelSettingsReader.GetModelAsync(_appSettings, ct);
            var analysis = await _visionAnalyzer.AnalyzeAsync(imageBase64, ragContext, exclusionNames, visionModel, ct);

            if (analysis.Status != CardVisionStatus.Ok)
            {
                sw.Stop();
                return new ScanBatchItemDto(
                    fileName, archivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                    sw.ElapsedMilliseconds,
                    $"Analyse texte/vision indisponible ({analysis.Status}): {analysis.Diagnostic}");
            }

            var best = analysis.Results.OrderByDescending(r => r.Confidence).FirstOrDefault();
            if (best is null || string.IsNullOrWhiteSpace(best.DetectedName))
            {
                sw.Stop();
                return new ScanBatchItemDto(
                    fileName, archivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                    sw.ElapsedMilliseconds, "Analyse texte/vision : aucune carte lisible sur la photo.");
            }

            // Embedding DINOv2 de la même photo — réutilisé si le nom lu par llava matche plusieurs
            // cartes en base (ex. "Skitty" ~20 versions/sets) : DINOv2 tranche alors visuellement
            // entre CES candidats précis, sans coût CPU comparable à un 2e appel llava (voir
            // FindCardByDetectedNameAsync). Dégradation gracieuse : si l'embedding échoue, on garde
            // simplement le comportement précédent (candidat unique + confiance, sinon aucun match).
            var embeddingResult = await _embeddingService.EmbedAsync(imageBase64, ct);
            var embedding = embeddingResult.Status == CardEmbeddingStatus.Ok ? embeddingResult.Embedding : null;
            sw.Stop();

            var match = await FindCardByDetectedNameAsync(best, excludedCardIds, embedding, ct);
            if (match is null)
            {
                return new ScanBatchItemDto(
                    fileName, archivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                    sw.ElapsedMilliseconds,
                    $"Analyse texte/vision : \"{best.DetectedName}\" lu mais aucune correspondance fiable en catalogue.");
            }

            return new ScanBatchItemDto(
                fileName, archivedPhotoFileName, Success: true, match.Id, match.Name, match.Set?.Name,
                match.Set?.ReleaseDate?.Year, match.Number, match.ImageSmall, null,
                sw.ElapsedMilliseconds, Error: null);
        }
        catch (Exception ex)
        {
            if (sw.IsRunning) sw.Stop();
            _logger.LogWarning(ex, "[Scan batch] erreur fallback texte/vision sur {FileName}", fileName);
            return new ScanBatchItemDto(
                fileName, archivedPhotoFileName, Success: false, null, null, null, null, null, null, null,
                sw.ElapsedMilliseconds, ex.Message);
        }
    }

    /// <summary>
    /// Associe un résultat de vision à une carte en base. Reprend le principe de
    /// ScanCollectionCommandHandler.FindMatchAsync (désactivé côté scan mobile faute de
    /// traduction FR à l'époque) en l'étendant : cherche d'abord parmi les traductions FR
    /// (ICardTranslationRepository.SearchCardIdsByNameAsync — la carte physique est imprimée en
    /// français), puis en repli parmi les noms anglais du catalogue de base. Le numéro imprimé,
    /// quand lisible, est le signal prioritaire (plus fiable qu'un nom parfois tronqué/accentué
    /// par l'OCR). À défaut de numéro : si plusieurs candidats partagent le même nom (ex. "Skitty"
    /// ~20 versions/sets) et qu'un <paramref name="embedding"/> DINOv2 de la photo est disponible,
    /// on laisse DINOv2 arbitrer visuellement entre CES candidats précis (SearchByEmbeddingAmongAsync)
    /// plutôt que de renoncer — combine la lecture du nom (llava) et la discrimination visuelle
    /// (DINOv2) sans second appel llava. Sinon (pas d'embedding, ou candidat unique), on retombe
    /// sur l'ancien comportement : candidat unique + confiance de détection élevée uniquement.
    /// </summary>
    private async Task<PokemonCard?> FindCardByDetectedNameAsync(
        CardVisionResult result, IReadOnlyList<Guid> excludedCardIds, float[]? embedding, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(result.DetectedName))
            return null;

        var normalizedDetected = Normalize(result.DetectedName);
        if (normalizedDetected.Length == 0)
            return null;

        var candidates = new List<PokemonCard>();
        var seenIds = new HashSet<Guid>();

        // 1) Recherche dans les traductions, TOUTES langues confondues (pas de filtre "fr" en dur
        // — corrigé le 2026-09-11 : on ne sait pas à l'avance dans quelle langue la carte scannée
        // est imprimée, une carte EN passe par le même chemin qu'une carte FR).
        var translatedCardIds = await _translationRepo.SearchCardIdsByNameAsync(
            language: null, result.DetectedName.Trim(), TranslationCandidateLimit, ct);
        foreach (var id in translatedCardIds)
        {
            if (excludedCardIds.Contains(id) || !seenIds.Add(id))
                continue;
            var card = await _cardRepo.GetByIdAsync(id, ct);
            if (card is not null)
                candidates.Add(card);
        }

        // 2) Repli nom EN (catalogue de base) — couvre le cas d'une carte absente de
        // card_translations (pas encore synchronisée, ou déjà en anglais et jamais traduite).
        // SearchByNameIgnoreCaseAsync (ILIKE) et non SearchAsync (Contains, sensible à la casse
        // sous Postgres) : bug réel observé le 2026-09-11 — "Skitty" lu par le LLM vision n'était
        // jamais trouvé car le seed, déjà passé par Normalize() (minuscules), ne matchait pas le
        // nom "Skitty" (capitalisé) en base via une comparaison sensible à la casse.
        var seed = FirstSignificantWord(normalizedDetected);
        if (seed.Length > 0)
        {
            var items = await _cardRepo.SearchByNameIgnoreCaseAsync(seed, TextCandidatePageSize, ct);

            foreach (var card in items)
            {
                if (excludedCardIds.Contains(card.Id) || !seenIds.Add(card.Id))
                    continue;

                var normalizedName = Normalize(card.Name);
                if (normalizedName.Contains(normalizedDetected, StringComparison.Ordinal)
                    || normalizedDetected.Contains(normalizedName, StringComparison.Ordinal))
                    candidates.Add(card);
            }
        }

        if (candidates.Count == 0)
            return null;

        // Numéro imprimé = signal le plus fiable, prioritaire dès qu'il est lisible.
        if (!string.IsNullOrWhiteSpace(result.DetectedNumber))
        {
            var byNumber = candidates.FirstOrDefault(c => NumbersMatch(c.Number, result.DetectedNumber));
            if (byNumber is not null)
                return byNumber;
        }

        // Pas de numéro exploitable, plusieurs candidats pour ce nom (ex. "Skitty" ~20
        // versions/sets) : on laisse DINOv2 arbitrer visuellement entre CES candidats précis,
        // moins gourmand qu'un 2e appel llava — voir doc de la méthode.
        if (candidates.Count > 1 && embedding is not null)
        {
            var candidateIds = candidates.Select(c => c.Id).ToList();
            var visualMatches = await _cardRepo.SearchByEmbeddingAmongAsync(
                embedding, candidateIds, EmbeddingArbitrationTopN, ct);
            if (visualMatches.Count > 0)
                return visualMatches[0].Card;
        }

        // Pas de numéro lisible / pas de match exact / pas d'embedding disponible : on ne prend le
        // candidat unique que si la confiance de détection est élevée, pour éviter de proposer une
        // carte au hasard.
        return candidates.Count == 1 && result.Confidence >= VisionAutoMatchConfidence
            ? candidates[0]
            : null;
    }

    private static string FirstSignificantWord(string normalized)
    {
        var spaceIndex = normalized.IndexOf(' ');
        return spaceIndex > 0 ? normalized[..spaceIndex] : normalized;
    }

    /// <summary>Minuscules, sans accents, espaces normalisés — pour comparer un texte lu par le
    /// modèle vision à un nom stocké en base sans être sensible à la casse ou à l'accentuation.</summary>
    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsWhiteSpace(ch) ? ' ' : char.ToLowerInvariant(ch));
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Compare un numéro imprimé (ex "025/165" ou "025") au numéro stocké en base (ex "25").
    /// Les zéros de tête et la partie "/total" sont ignorés.
    /// </summary>
    private static bool NumbersMatch(string cardNumber, string detected)
    {
        static string Core(string value)
        {
            var head = value.Split('/')[0].Trim();
            var trimmed = head.TrimStart('0');
            return trimmed.Length == 0 ? head : trimmed;
        }

        if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(detected))
            return false;

        return string.Equals(Core(cardNumber), Core(detected), StringComparison.OrdinalIgnoreCase);
    }
}
