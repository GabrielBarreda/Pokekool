using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Application.Pokemon.Services;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.ScanCollection;

public sealed class ScanCollectionCommandHandler
    : IRequestHandler<ScanCollectionCommand, ScanCollectionResult>
{
    private const double MinConfidence = 0.4;
    private const double AutoMatchConfidence = 0.65;
    private const int CandidatePageSize = 20;
    private const int VisualMatchTopN = 5;

    private readonly IVaultKnowledgeService _vault;
    private readonly ICardVisionAnalyzer _analyzer;
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardEmbeddingService _embeddingService;
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;
    private readonly IAppSettingRepository _appSettings;
    private readonly ILogger<ScanCollectionCommandHandler> _logger;

    public ScanCollectionCommandHandler(
        IVaultKnowledgeService vault,
        ICardVisionAnalyzer analyzer,
        IPokemonCardRepository cardRepo,
        ICardEmbeddingService embeddingService,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage,
        IAppSettingRepository appSettings,
        ILogger<ScanCollectionCommandHandler> logger)
    {
        _vault = vault;
        _analyzer = analyzer;
        _cardRepo = cardRepo;
        _embeddingService = embeddingService;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<ScanCollectionResult> Handle(
        ScanCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var ragContext = await _vault.BuildPromptContextAsync(cancellationToken);
        var visionModel = await Pokekool.Application.Collection.Scanning.VisionModelSettingsReader
            .GetModelAsync(_appSettings, cancellationToken);
        var analysis = await _analyzer.AnalyzeAsync(
            request.ImageBase64, ragContext, request.Exclusions, visionModel, cancellationToken);

        // Échec technique de l'analyse : on remonte la cause précise plutôt qu'un vide silencieux.
        if (analysis.Status != CardVisionStatus.Ok)
        {
            var (status, message) = analysis.Status switch
            {
                CardVisionStatus.ModelNotFound => (ScanStatus.ModelNotFound,
                    analysis.Diagnostic ?? "Modèle d'analyse absent côté serveur."),
                CardVisionStatus.ServiceUnavailable => (ScanStatus.ServiceUnavailable,
                    analysis.Diagnostic ?? "Service d'analyse indisponible."),
                _ => (ScanStatus.InvalidResponse,
                    analysis.Diagnostic ?? "Réponse d'analyse illisible."),
            };
            return new ScanCollectionResult(Array.Empty<CardScanMatchDto>(), status, message, RawDetections: 0);
        }

        var visionResults = analysis.Results;
        var rawCount = visionResults.Count;

        // ─────────────────────────────────────────────────────────────────────────────
        // TEST (2026-09-08) : matching texte débranché au profit du matching visuel pur
        // (DINOv2), le temps d'évaluer ce workflow seul, sans que le texte ne compense ses
        // ratés (langue de la carte différente du catalogue EN, OCR imprécis, etc.).
        // Le matching texte n'est PAS supprimé — juste court-circuité — voir FindMatchAsync
        // et LogVisualMatchComparisonAsync (renommée VisualMatchAsync) plus bas.
        // Pour revenir au comportement précédent (texte prioritaire, DINOv2 en log seul) :
        // remettre ce bloc à l'identique de son état avant le 2026-09-08.
        // ─────────────────────────────────────────────────────────────────────────────
        var visualMatches = await VisualMatchAsync(
            request.ImageBase64, request.ExcludedCardIds, _displayLanguage.Language, cancellationToken);

        var (resultStatus, resultMessage) = visualMatches.Count > 0
            ? (ScanStatus.Ok, (string?)null)
            : rawCount > 0
                ? (ScanStatus.LowConfidence,
                    "Aucun candidat visuel trouvé (catalogue non indexé, ou photo illisible).")
                : (ScanStatus.NoCardDetected,
                    "Aucune carte détectée sur la photo. Cadre une carte bien à plat, nette et éclairée.");

        return new ScanCollectionResult(visualMatches, resultStatus, resultMessage, rawCount);
    }

    /// <summary>
    /// TEST (2026-09-08) : matching par similarité visuelle (DINOv2) — voie PRINCIPALE du
    /// scan tant que le matching texte est débranché (voir Handle ci-dessus). Retourne les
    /// <see cref="VisualMatchTopN"/> candidats les plus proches, exposés tels quels au client
    /// (choix à l'utilisateur, pas de seuil d'auto-match pour l'instant — voir commentaire
    /// point 3 du chantier : affiner avec le texte pour départager des candidats proches).
    /// Ne doit jamais faire échouer le scan : toute erreur ici retourne une liste vide plutôt
    /// que de remonter une exception, au même titre que l'ancien comportement "log seul".
    /// </summary>
    private async Task<List<CardScanMatchDto>> VisualMatchAsync(
        string imageBase64, IReadOnlyList<Guid>? excludedCardIds, string displayLanguage, CancellationToken ct)
    {
        try
        {
            var embedding = await _embeddingService.EmbedAsync(imageBase64, ct);
            if (embedding.Status != CardEmbeddingStatus.Ok || embedding.Embedding is null)
            {
                _logger.LogInformation(
                    "[Scan visuel DINOv2] embedding indisponible ({Status}) : {Diagnostic}",
                    embedding.Status, embedding.Diagnostic);
                return new List<CardScanMatchDto>();
            }

            var visualCandidates = await _cardRepo.SearchByEmbeddingAsync(
                embedding.Embedding, VisualMatchTopN, excludedCardIds, ct);

            if (visualCandidates.Count == 0)
            {
                _logger.LogInformation("[Scan visuel DINOv2] aucun candidat (catalogue non indexé ?)");
                return new List<CardScanMatchDto>();
            }

            _logger.LogInformation(
                "[Scan visuel DINOv2] top-{TopN}: {TopResults}",
                VisualMatchTopN,
                string.Join(" ; ", visualCandidates.Select(v => $"{v.Card.Name} ({v.Distance:F4})")));

            // Confiance affichée = 1 - distance cosine (0 = aucune ressemblance, 1 = identique).
            // Purement indicatif pour l'instant : pas encore de seuil d'auto-sélection calé sur
            // cette échelle (voir écart 0.13 vs 0.41 observé en test réel le 2026-09-08).
            var englishNamesByCardId = visualCandidates.ToDictionary(v => v.Card.Id, v => v.Card.Name);
            var displayNames = await _displayNameResolver.ResolveManyAsync(
                englishNamesByCardId, displayLanguage, ct);

            return visualCandidates
                .Select(v => new CardScanMatchDto(
                    CardId: v.Card.Id,
                    CardTcgId: v.Card.TcgId,
                    CardName: v.Card.Name,
                    DisplayName: displayNames.TryGetValue(v.Card.Id, out var displayName) ? displayName : v.Card.Name,
                    SetName: v.Card.Set?.Name,
                    CardNumber: v.Card.Number,
                    ImageSmall: v.Card.ImageSmall,
                    GradingCompany: null,
                    GradeValue: null,
                    CertNumber: null,
                    Confidence: Math.Clamp(1.0 - v.Distance, 0.0, 1.0),
                    Matched: true))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Scan visuel DINOv2] erreur pendant le matching visuel");
            return new List<CardScanMatchDto>();
        }
    }

    /// <summary>
    /// Associe un résultat de vision à une carte en base — l'étape qui fait la reconnaissance
    /// au final, puisque le modèle vision se contente de transcrire ce qui est imprimé.
    /// Robuste à deux problèmes réels observés en test boutique :
    /// 1) Le nom "tel qu'imprimé" peut inclure un sous-titre absent du nom canonique en base
    ///    (ex: carte imprimée "Pokédex HANDY910is", nom en base "Pokédex") — une simple
    ///    comparaison "le nom en base contient la recherche" échoue dans ce sens.
    /// 2) Le numéro imprimé (ex "114/127") est un signal bien plus fiable qu'un nom parfois
    ///    accentué/tronqué par l'OCR — on lui donne la priorité dès qu'il est lisible, au lieu
    ///    de ne le regarder que parmi les candidats d'une recherche par nom déjà infructueuse.
    /// </summary>
#if false
    // TEST (2026-09-08) : matching texte débranché, voir Handle + VisualMatchAsync ci-dessus.
    // Conservé intact pour être rebranché lors du chantier "affinage textuel des candidats
    // visuels proches" (point 3). Ne pas supprimer.
    private async Task<PokemonCard?> FindMatchAsync(CardVisionResult result, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(result.DetectedName))
            return null;

        var normalizedDetected = Normalize(result.DetectedName);
        if (normalizedDetected.Length == 0)
            return null;

        // Amorce de recherche sur le premier mot significatif du nom détecté — quasi toujours
        // un sous-ensemble fiable du nom canonique même quand un sous-titre est imprimé à côté.
        var seed = FirstSignificantWord(normalizedDetected);
        if (seed.Length == 0)
            return null;

        var (items, _) = await _cardRepo.SearchAsync(
            search: seed,
            setId: null,
            rarity: null,
            supertype: null,
            page: 1,
            pageSize: CandidatePageSize,
            ct: ct);

        if (items.Count == 0)
            return null;

        // Comparaison bidirectionnelle et insensible aux accents/casse : couvre aussi bien un
        // nom détecté plus long que le nom en base (sous-titre imprimé) que l'inverse.
        var candidates = items
            .Where(c =>
            {
                var normalizedName = Normalize(c.Name);
                return normalizedName.Contains(normalizedDetected, StringComparison.Ordinal)
                    || normalizedDetected.Contains(normalizedName, StringComparison.Ordinal);
            })
            .ToList();
        if (candidates.Count == 0)
            candidates = items.ToList();

        if (!string.IsNullOrWhiteSpace(result.DetectedNumber))
        {
            var byNumber = candidates.FirstOrDefault(c => NumbersMatch(c.Number, result.DetectedNumber));
            if (byNumber is not null)
                return byNumber;
        }

        // Pas de numéro lisible, ou aucun match exact sur le numéro :
        // on prend le meilleur candidat uniquement si la confiance est élevée.
        return result.Confidence >= AutoMatchConfidence ? candidates[0] : null;
    }
#endif

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
