using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Collection;

/// <summary>
/// Un run d'analyse de dossier (scan-batch) — remplace l'ancien historique fichier
/// (scan-batch/_history/_index.jsonl, non persisté, purement local à l'instance API) par une
/// vraie table Postgres, pour garder une traçabilité dans le temps de ce qui a été scanné :
/// collection propre de la boutique, ou collection tierce apportée par un vendeur potentiel
/// (rachat, estimation...). Voir StartScanRunCommand pour la création (popup de destination),
/// et RunScanBatchCommand pour le remplissage des items après analyse DINOv2.
/// </summary>
public sealed class ScanRun : Entity
{
    public DateTimeOffset RunAt { get; private set; }
    public ScanDestinationType DestinationType { get; private set; }

    /// <summary>Pertinent uniquement si DestinationType = Boutique — les cartes validées
    /// ont-elles été ajoutées directement au stock ?</summary>
    public bool? AddedToStockDirectly { get; private set; }

    /// <summary>Pertinent uniquement si DestinationType = Tiers.</summary>
    public string? ThirdPartyName { get; private set; }
    public ScanReason? Reason { get; private set; }
    public string? ReasonOtherText { get; private set; }

    public int TotalPhotos { get; private set; }
    public int Succeeded { get; private set; }
    public int Failed { get; private set; }
    public double? AverageDistance { get; private set; }
    public long AverageElapsedMs { get; private set; }

    /// <summary>Pertinent uniquement si DestinationType = Boutique — classeur (StorageBinder,
    /// non rangé) auto-créé pour ce run, où les cartes reconnues ont été ajoutées au stock.
    /// Voir RunScanBatchCommandHandler.</summary>
    public Guid? CreatedBinderId { get; private set; }

    private readonly List<ScanRunItem> _items = new();
    public IReadOnlyCollection<ScanRunItem> Items => _items;

    private ScanRun() { }

    /// <summary>
    /// Ouvre un nouveau run — étape 1 du pipeline (popup "La collection appartient-elle à la
    /// boutique ?" puis, selon la réponse, "Ajouter directement au stock ?" ou nom + motif de la
    /// collection tierce). Les stats/items sont renseignés ensuite via CompleteWithResults, une
    /// fois l'analyse DINOv2 terminée.
    /// </summary>
    public static ScanRun Create(
        ScanDestinationType destinationType,
        bool? addedToStockDirectly,
        string? thirdPartyName,
        ScanReason? reason,
        string? reasonOtherText)
    {
        if (destinationType == ScanDestinationType.Boutique)
        {
            if (thirdPartyName is not null || reason is not null || reasonOtherText is not null)
                throw new Pokekool.Domain.Common.DomainException(
                    "scan_run.invalid",
                    "ThirdPartyName/Reason/ReasonOtherText ne s'appliquent qu'à une collection Tiers.");
        }
        else // Tiers
        {
            if (string.IsNullOrWhiteSpace(thirdPartyName))
                throw new Pokekool.Domain.Common.DomainException(
                    "scan_run.invalid", "ThirdPartyName est requis pour une collection tierce.");
            if (reason is null)
                throw new Pokekool.Domain.Common.DomainException(
                    "scan_run.invalid", "Reason est requis pour une collection tierce.");
            if (reason == ScanReason.Autre && string.IsNullOrWhiteSpace(reasonOtherText))
                throw new Pokekool.Domain.Common.DomainException(
                    "scan_run.invalid", "ReasonOtherText est requis quand Reason = Autre.");
            if (addedToStockDirectly is not null)
                throw new Pokekool.Domain.Common.DomainException(
                    "scan_run.invalid", "AddedToStockDirectly ne s'applique qu'à une collection Boutique.");
        }

        return new ScanRun
        {
            Id                    = Guid.NewGuid(),
            RunAt                 = DateTimeOffset.UtcNow,
            DestinationType       = destinationType,
            AddedToStockDirectly  = addedToStockDirectly,
            ThirdPartyName        = thirdPartyName?.Trim(),
            Reason                = reason,
            ReasonOtherText       = reason == ScanReason.Autre ? reasonOtherText!.Trim() : null,
            CreatedAt             = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Renseigne les stats globales + items une fois l'analyse DINOv2 terminée
    /// (RunScanBatchCommandHandler). Les ScanRunItem sont ajoutés séparément par le repository
    /// (EF Core suit la navigation Items via AddRangeAsync côté handler).</summary>
    public void CompleteWithResults(int totalPhotos, int succeeded, int failed, double? averageDistance, long averageElapsedMs)
    {
        TotalPhotos      = totalPhotos;
        Succeeded        = succeeded;
        Failed           = failed;
        AverageDistance  = averageDistance;
        AverageElapsedMs = averageElapsedMs;
        UpdatedAt        = DateTimeOffset.UtcNow;
    }

    public void AddItem(ScanRunItem item) => _items.Add(item);

    /// <summary>Rattache le classeur auto-créé pour ce run boutique — voir
    /// RunScanBatchCommandHandler, appelé une fois le StorageBinder + les ShopStock créés.</summary>
    public void SetCreatedBinder(Guid binderId)
    {
        if (DestinationType != ScanDestinationType.Boutique)
            throw new Pokekool.Domain.Common.DomainException(
                "scan_run.invalid", "CreatedBinderId ne s'applique qu'à une collection Boutique.");

        CreatedBinderId = binderId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
