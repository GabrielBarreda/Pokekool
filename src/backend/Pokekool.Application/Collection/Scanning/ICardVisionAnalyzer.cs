namespace Pokekool.Application.Collection.Scanning;

/// <summary>
/// Analyse une image (photo/vidéo d'une ou plusieurs cartes Pokémon) via un LLM Vision.
/// Implémenté dans Infrastructure (Ollama Vision). Dégradation gracieuse : en cas d'échec,
/// retourne une liste vide plutôt que de lever une exception.
/// </summary>
public interface ICardVisionAnalyzer
{
    Task<IReadOnlyList<CardVisionResult>> AnalyzeAsync(
        string imageBase64,
        string ragContext,
        CancellationToken ct = default);
}

/// <summary>Carte détectée par le LLM Vision, avant matching en base.</summary>
public sealed record CardVisionResult(
    string? DetectedName,
    string? DetectedSetName,
    string? DetectedNumber,
    string? GradingCompany,   // "PSA" | "BGS" | "CGC" | null
    decimal? GradeValue,      // ex: 9.5 — null si carte brute
    string? CertNumber,       // numéro de certification visible sur le slab
    double Confidence         // 0.0–1.0
);
