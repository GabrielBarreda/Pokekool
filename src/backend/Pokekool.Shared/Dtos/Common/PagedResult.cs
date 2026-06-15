namespace Pokekool.Shared.Dtos.Common;

/// <summary>
/// Enveloppe de pagination standard exposée par tous les endpoints qui listent.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages);
