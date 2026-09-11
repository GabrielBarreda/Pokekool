using Microsoft.Extensions.Logging;
using Pokekool.Domain.Entities.Collection;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;

namespace Pokekool.Application.Collection.Scanning;

/// <summary>
/// Commit des cartes reconnues d'un ScanRun (destination Boutique) vers le stock — extrait de
/// RunScanBatchCommandHandler pour être appelable à deux moments distincts : automatiquement à la
/// fin de l'analyse si AddedToStockDirectly = true (comportement "rapide", sans vérification), ou
/// explicitement via le bouton "🏪 Ajouter au stock" du récap une fois les corrections
/// (refuser/relancer) terminées. Idempotent : no-op si CreatedBinderId est déjà renseigné, pour
/// éviter tout doublon (cf. bug remonté : relancer un run refusé dédoublait le stock).
/// </summary>
public interface IScanRunStockCommitter
{
    /// <summary>Crée un classeur non rangé + un ShopStock par carte reconnue de ce run, puis
    /// rattache le classeur au ScanRun (SetCreatedBinder). Ne fait rien si le run n'est pas
    /// Boutique, n'a aucune carte reconnue, ou a déjà un CreatedBinderId (déjà commité).
    /// Ne sauvegarde PAS via IUnitOfWork — à la charge de l'appelant (même transaction que le
    /// reste du handler appelant).</summary>
    /// <returns>Nombre de cartes ajoutées au stock (0 si no-op).</returns>
    Task<int> CommitAsync(ScanRun scanRun, CancellationToken ct = default);
}

public sealed class ScanRunStockCommitter : IScanRunStockCommitter
{
    private readonly IShopRepository _shopRepo;
    private readonly IShopStockRepository _stockRepo;
    private readonly IStorageBinderRepository _binderRepo;
    private readonly IStorageLocationRepository _locationRepo;
    private readonly ILogger<ScanRunStockCommitter> _logger;

    public ScanRunStockCommitter(
        IShopRepository shopRepo,
        IShopStockRepository stockRepo,
        IStorageBinderRepository binderRepo,
        IStorageLocationRepository locationRepo,
        ILogger<ScanRunStockCommitter> logger)
    {
        _shopRepo = shopRepo;
        _stockRepo = stockRepo;
        _binderRepo = binderRepo;
        _locationRepo = locationRepo;
        _logger = logger;
    }

    public async Task<int> CommitAsync(ScanRun scanRun, CancellationToken ct = default)
    {
        if (scanRun.DestinationType != ScanDestinationType.Boutique)
            return 0;

        // Idempotence — un classeur a déjà été créé pour ce run, on ne recrée jamais de stock
        // en double (cf. bug : "si je scan et relance pour 1 refus il va me dédoubler le stock").
        if (scanRun.CreatedBinderId is not null)
        {
            _logger.LogInformation(
                "[Scan batch] run {ScanRunId} déjà commité au stock (classeur {BinderId}) — no-op.",
                scanRun.Id, scanRun.CreatedBinderId);
            return 0;
        }

        var matchedCardIds = scanRun.Items
            .Where(i => i.Success && i.BestCardId.HasValue)
            .Select(i => i.BestCardId!.Value)
            .Distinct()
            .ToList();

        if (matchedCardIds.Count == 0)
            return 0;

        var shop = await _shopRepo.GetOrCreateAsync(ct);

        var label = $"Scan {scanRun.RunAt.ToLocalTime():dd/MM/yyyy HH:mm}";
        var binder = StorageBinder.CreateUnplaced(label);
        await _binderRepo.AddAsync(binder, ct);

        foreach (var cardId in matchedCardIds)
        {
            var stock = ShopStock.Create(
                shopId: shop.Id,
                cardId: cardId,
                productName: null,
                type: ShopProductType.CardRaw,
                language: "FR",
                condition: null,
                gradingCompany: null,
                gradeValue: null,
                quantityTotal: 1,
                quantityAvailable: 1,
                quantityReserved: 0,
                purchasePrice: null,
                purchaseCurrency: null,
                notes: "Créé via scan de dossier");

            await _stockRepo.AddAsync(stock, ct);

            var location = StorageLocation.Create(binder.Id, StorageLocation.OwnerShopStock, stock.Id, null);
            await _locationRepo.AddAsync(location, ct);
        }

        scanRun.SetCreatedBinder(binder.Id);

        _logger.LogInformation(
            "[Scan batch] run {ScanRunId} (Boutique) — {Count} carte(s) ajoutée(s) au stock, classeur {BinderId} créé.",
            scanRun.Id, matchedCardIds.Count, binder.Id);

        return matchedCardIds.Count;
    }
}
