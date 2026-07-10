using MediatR;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.ScanCollection;

public sealed class ScanCollectionCommandHandler
    : IRequestHandler<ScanCollectionCommand, ScanCollectionResult>
{
    private const double MinConfidence = 0.4;
    private const double AutoMatchConfidence = 0.65;

    private readonly IVaultKnowledgeService _vault;
    private readonly ICardVisionAnalyzer _analyzer;
    private readonly IPokemonCardRepository _cardRepo;

    public ScanCollectionCommandHandler(
        IVaultKnowledgeService vault,
        ICardVisionAnalyzer analyzer,
        IPokemonCardRepository cardRepo)
    {
        _vault = vault;
        _analyzer = analyzer;
        _cardRepo = cardRepo;
    }

    public async Task<ScanCollectionResult> Handle(
        ScanCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var ragContext = await _vault.BuildPromptContextAsync(cancellationToken);
        var visionResults = await _analyzer.AnalyzeAsync(request.ImageBase64, ragContext, cancellationToken);

        var matches = new List<CardScanMatchDto>();

        foreach (var result in visionResults)
        {
            if (result.Confidence < MinConfidence)
                continue;

            PokemonCard? matched = null;

            if (!string.IsNullOrWhiteSpace(result.DetectedName))
            {
                var (items, _) = await _cardRepo.SearchAsync(
                    search: result.DetectedName,
                    setId: null,
                    rarity: null,
                    supertype: null,
                    page: 1,
                    pageSize: 5,
                    ct: cancellationToken);

                if (items.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(result.DetectedNumber))
                        matched = items.FirstOrDefault(c => NumbersMatch(c.Number, result.DetectedNumber));

                    // Pas de numéro lisible, ou aucun match exact sur le numéro :
                    // on prend le meilleur candidat uniquement si la confiance est élevée.
                    if (matched is null && result.Confidence >= AutoMatchConfidence)
                        matched = items[0];
                }
            }

            matches.Add(matched is not null
                ? new CardScanMatchDto(
                    CardId: matched.Id,
                    CardName: matched.Name,
                    SetName: matched.Set?.Name,
                    CardNumber: matched.Number,
                    ImageSmall: matched.ImageSmall,
                    GradingCompany: result.GradingCompany,
                    GradeValue: result.GradeValue,
                    CertNumber: result.CertNumber,
                    Confidence: result.Confidence,
                    Matched: true)
                : new CardScanMatchDto(
                    CardId: null,
                    CardName: result.DetectedName,
                    SetName: result.DetectedSetName,
                    CardNumber: result.DetectedNumber,
                    ImageSmall: null,
                    GradingCompany: result.GradingCompany,
                    GradeValue: result.GradeValue,
                    CertNumber: result.CertNumber,
                    Confidence: result.Confidence,
                    Matched: false));
        }

        return new ScanCollectionResult(matches);
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
