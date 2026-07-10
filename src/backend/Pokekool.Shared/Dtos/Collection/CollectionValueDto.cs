namespace Pokekool.Shared.Dtos.Collection;

public sealed record CollectionValueDto(
    decimal TotalValue,
    string Currency,
    int CardCount);
