namespace Pokekool.Shared.Dtos.Common;

/// <summary>
/// Représentation RFC 7807 des erreurs renvoyées par l'API.
/// </summary>
public sealed record ProblemDetailsDto(
    string Type,
    string Title,
    int Status,
    string? Detail,
    string? Instance,
    string? TraceId,
    IReadOnlyDictionary<string, string[]>? Errors);
