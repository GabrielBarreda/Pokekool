using Microsoft.Extensions.Logging;
using Pokekool.Application.DataCollection;

namespace Pokekool.Infrastructure.DataProviders.CardMarket;

/// <summary>
/// Provider CardMarket — prix EUR (LOW, TREND, AVG) depuis le marché européen.
/// Nécessite que pokemon_cards.cm_product_id soit mappé via la commande MapCardMarket.
/// Cartes sans cm_product_id sont ignorées silencieusement.
/// </summary>
internal sealed class CardMarketProvider : ICardPriceProvider
{
    public string Name => "cardmarket";
    public ProviderCapability Capabilities =>
        ProviderCapability.MarketPrices;

    private readonly ICardMarketClient _client;
    private readonly ILogger<CardMarketProvider> _logger;

    public CardMarketProvider(ICardMarketClient client, ILogger<CardMarketProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CardPriceData?> GetPriceAsync(
        string cardIdentifier, CancellationToken ct = default)
    {
        var results = await GetPricesBatchAsync([cardIdentifier], ct);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<IReadOnlyList<CardPriceData>> GetPricesBatchAsync(
        IEnumerable<string> cardIdentifiers, CancellationToken ct = default)
    {
        // cardIdentifier = "{cm_product_id}" (int sous forme de string)
        // Le handler SyncCardPricesCommandHandler construit les identifiants à partir de cm_product_id

        var ids = cardIdentifiers
            .Select(id => int.TryParse(id, out var n) ? (int?)n : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        if (ids.Count == 0)
        {
            _logger.LogWarning("CardMarketProvider: aucun cm_product_id valide fourni");
            return [];
        }

        // Regroupe par expansion pour minimiser les appels API
        // Pour MVP : on récupère les singles par cm_expansion_id
        // Les identifiers sont des cm_product_ids — on les groupe par expansion côté handler
        // Ici on reçoit directement les IDs et on cherche dans les singles par expansion

        // Approach MVP : 1 appel par expansion (fourni par le handler via le format "{expansionId}:{productId}")
        var results = new List<CardPriceData>();

        // Format attendu des identifiers : "expansionId:productId"
        var byExpansion = cardIdentifiers
            .Select(id =>
            {
                var parts = id.Split(':');
                if (parts.Length == 2
                    && int.TryParse(parts[0], out var expId)
                    && int.TryParse(parts[1], out var prodId))
                    return (ExpansionId: expId, ProductId: prodId, Raw: id);
                return default;
            })
            .Where(x => x != default)
            .GroupBy(x => x.ExpansionId)
            .ToList();

        foreach (var group in byExpansion)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var singles = await _client.GetSinglesAsync(group.Key, ct);
                var productIdSet = group.Select(x => x.ProductId).ToHashSet();

                foreach (var single in singles.Where(s => productIdSet.Contains(s.IdProduct)))
                {
                    if (single.PriceGuide is null) continue;

                    results.Add(new CardPriceData(
                        Source:         Name,
                        RawMarketPrice: single.PriceGuide.Trend ?? single.PriceGuide.Avg,
                        Currency:       "EUR",
                        FetchedAt:      DateTime.UtcNow));
                }

                // Rate limiting respectueux
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CardMarketProvider: erreur expansion {ExpansionId}", group.Key);
            }
        }

        return results;
    }
}
