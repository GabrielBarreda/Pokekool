using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;
using ProviderKeys = Pokekool.Domain.Entities.Pokemon.ProviderKeys;

namespace Pokekool.Infrastructure.DataProviders.TcgPlayer;

/// <summary>
/// Provider TCGPlayer — prix marché US (USD) via l'API REST TCGPlayer v1.37.0.
///
/// Stratégie d'identification :
///   cardIdentifier = TcgId Pokémon TCG ("base1-4").
///   La carte doit avoir pokemon_cards.tcgplayer_product_id mappé pour être récupérée.
///   Les cartes sans mapping sont ignorées silencieusement.
///
/// Batch : les productIds sont regroupés en appels /pricing/product/{ids} (max 250/requête).
/// </summary>
internal sealed class TcgPlayerProvider : ICardPriceProvider, IDisposable
{
    public string Name => "tcgplayer";

    public ProviderCapability Capabilities => ProviderCapability.MarketPrices;

    private readonly ITcgPlayerClient           _client;
    private readonly IPokemonCardRepository     _cardRepo;
    private readonly IProviderMappingRepository _mappingRepo;
    private readonly ILogger<TcgPlayerProvider> _logger;
    // Sémaphore partagé via DbConcurrencyGuard (scoped) — sérialise les accès DB
    // entre tous les providers qui tournent en parallèle dans DataAggregator.
    private readonly SemaphoreSlim _dbSemaphore;

    public TcgPlayerProvider(
        ITcgPlayerClient           client,
        IPokemonCardRepository     cardRepo,
        IProviderMappingRepository mappingRepo,
        ILogger<TcgPlayerProvider> logger,
        DbConcurrencyGuard         dbGuard)
    {
        _client      = client;
        _cardRepo    = cardRepo;
        _mappingRepo = mappingRepo;
        _logger      = logger;
        _dbSemaphore = dbGuard.Semaphore;
    }

    public void Dispose() { /* _dbSemaphore appartient au DbConcurrencyGuard scoped */ }

    // ── ICardPriceProvider ────────────────────────────────────────────────────

    public async Task<CardPriceData?> GetPriceAsync(
        string cardIdentifier, CancellationToken ct = default)
    {
        var results = await GetPricesBatchAsync([cardIdentifier], ct);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<IReadOnlyList<CardPriceData>> GetPricesBatchAsync(
        IEnumerable<string> cardIdentifiers, CancellationToken ct = default)
    {
        var tcgIds = cardIdentifiers.ToList();
        if (tcgIds.Count == 0) return [];

        // ── 1. Résoudre TcgId → TcgPlayerProductId ───────────────────────────
        var productIdToTcgId = new Dictionary<int, string>();

        foreach (var tcgId in tcgIds)
        {
            // Break plutôt que throw : retourne les résultats partiels au lieu de propager OCE
            if (ct.IsCancellationRequested) break;

            try
            {
                PokemonCard? card;
                await _dbSemaphore.WaitAsync(ct);
                try
                {
                    card = await _cardRepo.GetByTcgIdAsync(tcgId, ct);
                }
                finally
                {
                    _dbSemaphore.Release();
                }

                if (card is null) continue;

                var productIdStr = await _mappingRepo.GetCardValueAsync(
                    card.Id, ProviderKeys.TcgPlayer, ProviderKeys.ProductId, ct);

                if (productIdStr is null || !int.TryParse(productIdStr, out var productId))
                {
                    _logger.LogDebug(
                        "TcgPlayerProvider — carte '{TcgId}' sans tcgplayer product_id, ignorée", tcgId);
                    continue;
                }

                productIdToTcgId[productId] = tcgId;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex, "TcgPlayerProvider — erreur résolution '{TcgId}'", tcgId);
            }
        }

        if (productIdToTcgId.Count == 0)
        {
            _logger.LogWarning(
                "TcgPlayerProvider — aucun tcgplayer_product_id résolu parmi {Count} cartes",
                tcgIds.Count);
            return [];
        }

        // ── 2. Récupérer les prix (batchés par 250 dans TcgPlayerClient) ─────
        List<TcgPlayerPrice> prices;
        try
        {
            prices = await _client.GetPricesAsync(productIdToTcgId.Keys, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "TcgPlayerProvider — erreur API /pricing/product pour {Count} produits",
                productIdToTcgId.Count);
            return [];
        }

        // ── 3. Mapper vers CardPriceData ──────────────────────────────────────
        // TCGPlayer retourne plusieurs lignes par productId (Normal, Holofoil, Reverse Holofoil…)
        // On prend la ligne "Normal" en priorité, puis la première disponible.
        var pricesByProductId = prices
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<CardPriceData>();

        foreach (var (productId, _) in productIdToTcgId)
        {
            if (!pricesByProductId.TryGetValue(productId, out var variants))
                continue;

            // Préférer "Normal", sinon première variante disponible
            var best = variants.FirstOrDefault(v =>
                string.Equals(v.SubTypeName, "Normal", StringComparison.OrdinalIgnoreCase))
                ?? variants.FirstOrDefault(v => v.MarketPrice.HasValue)
                ?? variants[0];

            if (best.MarketPrice is null && best.LowPrice is null)
            {
                _logger.LogDebug(
                    "TcgPlayerProvider — productId {Id} sans prix disponible", productId);
                continue;
            }

            results.Add(new CardPriceData(
                Source:         Name,
                RawMarketPrice: best.MarketPrice ?? best.MidPrice ?? best.LowPrice,
                Currency:       "USD",
                FetchedAt:      DateTime.UtcNow));
        }

        _logger.LogInformation(
            "TcgPlayerProvider — {ResultCount}/{RequestCount} prix récupérés",
            results.Count, tcgIds.Count);

        return results;
    }
}
