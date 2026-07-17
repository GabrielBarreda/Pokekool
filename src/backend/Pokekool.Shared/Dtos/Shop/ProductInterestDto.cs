namespace Pokekool.Shared.Dtos.Shop;

/// <summary>Nombre de clients intéressés (demandes d'achat actives) par un article du présentoir.</summary>
public sealed record ProductInterestDto(Guid ProductId, int InterestedCount);
