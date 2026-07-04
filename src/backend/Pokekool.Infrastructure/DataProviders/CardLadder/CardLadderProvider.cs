using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.DataProviders.CardLadder;

/// <summary>
/// Provider CardLadder — prix gradés (PSA/BGS/CGC/SGC) via Firestore REST API.
///
/// Remplace la Phase 1 (Playwright CSS sur la page de recherche) qui était non fonctionnelle :
/// chaque grade ayant sa propre page CardLadder, la page de recherche n'expose pas les prix
/// par grade. L'API Firestore publique de CardLadder (projet cardladder-71d53) retourne
/// tous les grades d'une carte en une seule requête, avec le prix "Last Sold" (currentValue).
///
/// Pipeline :
///   1. runQuery Firestore par (name, number[, year]) → documents = grades de la carte
///   2. Suivre les champs "otherGrades" pour récupérer TOUS les grades disponibles
///   3. Mapper vers GradedPriceData[] → card_graded_prices
///
/// Avantages vs Playwright CSS :
///   • Aucun navigateur headless — rapide (< 500ms par carte)
///   • Données structurées JSON natives — pas de parsing HTML fragile
///   • Tous les grades en 1–2 requêtes vs N requêtes de pages individuelles
///   • Inclut pop report (population) — extensible vers card_populations
///
/// Voir : API-CardLadder.md dans le vault Obsidian
/// </summary>
public sealed class CardLadderProvider : ICardPriceProvider, IDisposable
{
    public string Name         => "cardladder";
    public ProviderCapability Capabilities =>
        ProviderCapability.GradedPrices;
    // Pas de MarketPrices : CardLadder ne fournit pas de prix brut non-gradé.

    private readonly CardLadderFirestoreClient   _firestore;
    private readonly IPokemonCardRepository      _cardRepo;
    private readonly IPokemonSetRepository       _setRepo;
    private readonly ILogger<CardLadderProvider> _logger;
    private readonly Random                      _rng         = new();
    private readonly SemaphoreSlim               _dbSemaphore;

    public CardLadderProvider(
        CardLadderFirestoreClient   firestore,
        IPokemonCardRepository      cardRepo,
        IPokemonSetRepository       setRepo,
        ILogger<CardLadderProvider> logger,
        DbConcurrencyGuard          dbGuard)
    {
        _firestore   = firestore;
        _cardRepo    = cardRepo;
        _setRepo     = setRepo;
        _logger      = logger;
        _dbSemaphore = dbGuard.Semaphore;
    }

    public void Dispose() { /* _dbSemaphore appartient au DbConcurrencyGuard scoped */ }

    // ── ICardPriceProvider ────────────────────────────────────────────────────

    public async Task<CardPriceData?> GetPriceAsync(
        string cardIdentifier, CancellationToken ct = default)
    {
        var (cardName, cardNumber, setYear) = await ResolveCardInfoAsync(cardIdentifier, ct);
        if (cardName is null || cardNumber is null)
        {
            _logger.LogWarning("CardLadder — infos introuvables pour '{Id}'", cardIdentifier);
            return null;
        }

        _logger.LogInformation(
            "CardLadder Firestore — '{Name} #{Number}' (set {Year})",
            cardName, cardNumber, setYear?.ToString() ?? "?");

        try
        {
            var gradeDocs = await _firestore.FindGradesForCardAsync(
                cardName, cardNumber, setYear, ct);

            if (gradeDocs.Count == 0)
            {
                _logger.LogWarning(
                    "CardLadder Firestore — aucune donnée pour '{Id}'", cardIdentifier);
                return null;
            }

            return MapToCardPriceData(gradeDocs);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CardLadder Firestore — erreur pour '{Id}'", cardIdentifier);
            return null;
        }
    }

    public async Task<IReadOnlyList<CardPriceData>> GetPricesBatchAsync(
        IEnumerable<string> cardIdentifiers, CancellationToken ct = default)
    {
        var results = new List<CardPriceData>();

        foreach (var id in cardIdentifiers)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var price = await GetPriceAsync(id, ct);
                if (price is not null) results.Add(price);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CardLadder Firestore — erreur ignorée pour '{Id}'", id);
            }

            // Rate limiting : Firestore est tolérant mais on reste respectueux
            await Task.Delay(_rng.Next(300, 800), ct);
        }

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(string? CardName, string? CardNumber, int? SetYear)> ResolveCardInfoAsync(
        string tcgId, CancellationToken ct)
    {
        await _dbSemaphore.WaitAsync(ct);
        try
        {
            var card = await _cardRepo.GetByTcgIdAsync(tcgId, ct);
            if (card is null) return (null, null, null);

            var lastDash  = tcgId.LastIndexOf('-');
            var setPrefix = lastDash > 0 ? tcgId[..lastDash] : tcgId;

            // Utiliser card.Number (numéro imprimé sur la carte, ex: "1" pour Clefable Jungle)
            // et NON le suffixe du tcgId (ex: "17" pour base2-17 qui est l'index API pokemontcg.io).
            // CardLadder indexe par numéro imprimé visible sur la carte.
            var cardNumber = card.Number;

            var set     = await _setRepo.GetByTcgIdAsync(setPrefix, ct);
            var setYear = set?.ReleaseDate?.Year;

            return (card.Name, cardNumber, setYear);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CardLadder — DB lookup échoué pour '{Id}'", tcgId);
            return (null, null, null);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    private static CardPriceData MapToCardPriceData(List<CardLadderGradeDoc> docs)
    {
        var grades = docs
            .Where(d => d.CurrentValue > 0 && !string.IsNullOrWhiteSpace(d.Company))
            .Select(d => new GradedPriceData(d.Company, d.GradeValue, d.CurrentValue))
            .ToList();

        return new CardPriceData(
            Source:         "cardladder",
            RawMarketPrice: null,
            GradedPrices:   grades.Count > 0 ? grades : null,
            Currency:       "USD",
            FetchedAt:      DateTime.UtcNow);
    }
}
