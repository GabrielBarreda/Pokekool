using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Collection;

/// <summary>
/// Une photo analysée au sein d'un ScanRun — reprend les champs de l'ancien ScanBatchItemDto
/// (fichier local) mais persisté en base, rattaché à son run pour la traçabilité (voir ScanRun).
/// </summary>
public sealed class ScanRunItem : Entity
{
    public Guid ScanRunId { get; private set; }

    public string FileName { get; private set; } = string.Empty;
    public string? ArchivedPhotoFileName { get; private set; }
    public bool Success { get; private set; }

    public Guid? BestCardId { get; private set; }
    public string? BestCardName { get; private set; }
    public string? BestSetName { get; private set; }
    public int? BestSetReleaseYear { get; private set; }
    public string? BestCardNumber { get; private set; }
    public string? BestImageSmall { get; private set; }
    public double? BestDistance { get; private set; }

    public long ElapsedMs { get; private set; }
    public string? Error { get; private set; }

    /// <summary>Historique cumulé des cartes proposées puis refusées par l'utilisateur pour
    /// cette photo, au fil des relances successives (bouton "Relancer les refusés") — sans ce
    /// cumul, chaque relance n'exclut que la dernière carte refusée et peut reproposer une carte
    /// déjà rejetée précédemment (ex: Gloom refusé → Erika's Jigglypuff refusé → Gloom reproposé).
    /// Voir RerunScanBatchItemsCommandHandler.</summary>
    private readonly List<Guid> _excludedCardIds = new();
    public IReadOnlyCollection<Guid> ExcludedCardIds => _excludedCardIds;

    /// <summary>Prix de marché estimé (RAW uniquement, pas de gradation) — renseigné
    /// uniquement pour un ScanRun de destination Tiers, voir SetEstimatedPrice et
    /// RunScanBatchCommandHandler.</summary>
    public decimal? EstimatedPrice { get; private set; }
    public string? EstimatedPriceCurrency { get; private set; }

    private ScanRunItem() { }

    public static ScanRunItem Create(
        Guid scanRunId,
        string fileName,
        string? archivedPhotoFileName,
        bool success,
        Guid? bestCardId,
        string? bestCardName,
        string? bestSetName,
        int? bestSetReleaseYear,
        string? bestCardNumber,
        string? bestImageSmall,
        double? bestDistance,
        long elapsedMs,
        string? error)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required", nameof(fileName));

        return new ScanRunItem
        {
            Id                     = Guid.NewGuid(),
            ScanRunId              = scanRunId,
            FileName               = fileName,
            ArchivedPhotoFileName  = archivedPhotoFileName,
            Success                = success,
            BestCardId             = bestCardId,
            BestCardName           = bestCardName,
            BestSetName            = bestSetName,
            BestSetReleaseYear     = bestSetReleaseYear,
            BestCardNumber         = bestCardNumber,
            BestImageSmall         = bestImageSmall,
            BestDistance           = bestDistance,
            ElapsedMs              = elapsedMs,
            Error                  = error,
            CreatedAt              = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Remplace le résultat d'une re-analyse ciblée (refus + exclusion) — voir
    /// RerunScanBatchItemsCommandHandler. Ne touche pas FileName/ArchivedPhotoFileName (identité
    /// de la photo dans le run).</summary>
    public void ReplaceResult(
        bool success,
        Guid? bestCardId,
        string? bestCardName,
        string? bestSetName,
        int? bestSetReleaseYear,
        string? bestCardNumber,
        string? bestImageSmall,
        double? bestDistance,
        long elapsedMs,
        string? error)
    {
        Success            = success;
        BestCardId         = bestCardId;
        BestCardName       = bestCardName;
        BestSetName        = bestSetName;
        BestSetReleaseYear = bestSetReleaseYear;
        BestCardNumber     = bestCardNumber;
        BestImageSmall     = bestImageSmall;
        BestDistance       = bestDistance;
        ElapsedMs          = elapsedMs;
        Error              = error;
        UpdatedAt          = DateTimeOffset.UtcNow;
    }

    /// <summary>Ajoute une carte à l'historique cumulé des refus pour cette photo (idempotent —
    /// ne duplique pas si déjà présente). Voir RerunScanBatchItemsCommandHandler : appelé avec
    /// la carte proposée AVANT la ré-analyse, pour que la nouvelle recherche l'exclue en plus de
    /// toutes celles déjà refusées lors des relances précédentes.</summary>
    public void AddExcludedCardId(Guid cardId)
    {
        if (!_excludedCardIds.Contains(cardId))
        {
            _excludedCardIds.Add(cardId);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Renseigne le prix de marché estimé (collection Tiers uniquement) — voir
    /// RunScanBatchCommandHandler, appelé après résolution du meilleur prix RAW disponible en
    /// catalogue pour BestCardId.</summary>
    public void SetEstimatedPrice(decimal? price, string? currency)
    {
        EstimatedPrice         = price;
        EstimatedPriceCurrency = currency;
        UpdatedAt               = DateTimeOffset.UtcNow;
    }
}
