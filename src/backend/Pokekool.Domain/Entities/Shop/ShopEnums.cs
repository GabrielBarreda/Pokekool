namespace Pokekool.Domain.Entities.Shop;

/// <summary>Nature d'un article vendu ou stocké par la boutique.</summary>
public enum ShopProductType
{
    CardRaw   = 1,
    CardGraded = 2,
    SealedSet = 3,
    SealedUpc = 4,
}

/// <summary>Cycle de vie d'un rendez-vous boutique.</summary>
public enum AppointmentStatus
{
    Pending    = 1,
    Confirmed  = 2,
    InProgress = 3,
    Completed  = 4,
    Cancelled  = 5,
}

/// <summary>Type de prestation proposée sur rendez-vous.</summary>
public enum ShopServiceType
{
    PreGrading  = 1,
    Restoration = 2,
    Purchase    = 3,
    Other       = 4,
}
