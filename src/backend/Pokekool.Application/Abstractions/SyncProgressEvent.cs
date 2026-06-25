namespace Pokekool.Application.Abstractions;

/// <summary>
/// Événement émis après le traitement d'une carte lors d'un sync prix.
/// Diffusé en SSE vers le frontend.
/// </summary>
public sealed record SyncProgressEvent(
    string   SetName,
    string   CardName,
    string   CardNumber,
    string[] Grades,      // ex. ["PSA7", "PSA9", "PSA10"]
    bool     Success,
    DateTime Timestamp);
