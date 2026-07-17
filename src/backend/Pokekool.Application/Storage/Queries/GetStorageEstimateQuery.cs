using MediatR;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

/// <summary>
/// Valeur estimée d'un carton ou d'un classeur : somme des dernières cotes des cartes
/// (stock boutique) qu'il contient. <c>OwnerType</c> ∈ { "box", "binder" }.
/// Réutilise la même logique de cote que le détail comptoir (gradée sinon marché).
/// </summary>
public sealed record GetStorageEstimateQuery(string OwnerType, Guid OwnerId)
    : IRequest<StorageEstimateDto>;

internal sealed class GetStorageEstimateQueryHandler
    : IRequestHandler<GetStorageEstimateQuery, StorageEstimateDto>
{
    private readonly IStorageBinderRepository _binderRepo;
    private readonly IStorageLocationRepository _locationRepo;
    private readonly IShopStockRepository _stockRepo;
    private readonly ICardMarketPriceRepository _marketRepo;
    private readonly ICardGradedPriceRepository _gradedRepo;

    public GetStorageEstimateQueryHandler(
        IStorageBinderRepository binderRepo, IStorageLocationRepository locationRepo,
        IShopStockRepository stockRepo, ICardMarketPriceRepository marketRepo,
        ICardGradedPriceRepository gradedRepo)
    {
        _binderRepo = binderRepo;
        _locationRepo = locationRepo;
        _stockRepo = stockRepo;
        _marketRepo = marketRepo;
        _gradedRepo = gradedRepo;
    }

    public async Task<StorageEstimateDto> Handle(GetStorageEstimateQuery request, CancellationToken ct)
    {
        // 1) Résout les classeurs concernés.
        IReadOnlyList<Guid> binderIds;
        if (string.Equals(request.OwnerType, "binder", StringComparison.OrdinalIgnoreCase))
        {
            binderIds = new[] { request.OwnerId };
        }
        else if (string.Equals(request.OwnerType, "box", StringComparison.OrdinalIgnoreCase))
        {
            var binders = await _binderRepo.ListByBoxAsync(request.OwnerId, ct);
            binderIds = binders.Select(b => b.Id).ToList();
        }
        else
        {
            return new StorageEstimateDto(null, null, 0);
        }

        if (binderIds.Count == 0) return new StorageEstimateDto(null, null, 0);

        // 2) Localisations "stock boutique" de ces classeurs.
        var locations = await _locationRepo.ListByBinderIdsAsync(binderIds, ct);
        var stockLocs = locations
            .Where(l => l.OwnerType == StorageLocation.OwnerShopStock)
            .ToList();

        // 3) Somme des cotes unitaires (cache par carte+grade pour éviter les requêtes répétées).
        decimal total = 0m;
        string? currency = null;
        var counted = 0;
        var cache = new Dictionary<string, (decimal? price, string? cur)>();

        foreach (var loc in stockLocs)
        {
            var stock = await _stockRepo.GetByIdAsync(loc.OwnerId, ct);
            if (stock?.CardId is not { } cardId) continue;

            var key = $"{cardId}|{stock.GradingCompany}|{stock.GradeValue}";
            if (!cache.TryGetValue(key, out var unit))
            {
                unit = await EstimateUnitAsync(cardId, stock, ct);
                cache[key] = unit;
            }

            if (unit.price is { } p)
            {
                total += p;
                currency ??= unit.cur;
                counted++;
            }
        }

        return new StorageEstimateDto(counted > 0 ? total : null, currency, counted);
    }

    /// <summary>Dernière cote unitaire d'une carte : gradée si l'article est gradé, sinon marché.</summary>
    private async Task<(decimal? price, string? cur)> EstimateUnitAsync(
        Guid cardId, ShopStock stock, CancellationToken ct)
    {
        var isGraded = !string.IsNullOrWhiteSpace(stock.GradingCompany) && stock.GradeValue is not null;
        if (isGraded)
        {
            var graded = await _gradedRepo.GetByCardIdAsync(cardId, ct);
            var latest = graded
                .Where(g => string.Equals(g.GradingCompany, stock.GradingCompany, StringComparison.OrdinalIgnoreCase)
                            && g.GradeValue == stock.GradeValue!.Value)
                .OrderByDescending(g => g.FetchedAt)
                .FirstOrDefault();
            return (latest?.Price, latest?.Currency);
        }

        var market = (await _marketRepo.GetByCardIdAsync(cardId, ct))
            .Where(m => m.MarketPrice.HasValue)
            .OrderByDescending(m => m.FetchedAt)
            .FirstOrDefault();
        return (market?.MarketPrice, market?.Currency);
    }
}
