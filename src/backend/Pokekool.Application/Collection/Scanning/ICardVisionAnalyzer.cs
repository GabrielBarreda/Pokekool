namespace Pokekool.Application.Collection.Scanning;

/// <summary>
/// Analyse une image (photo/vidéo d'une ou plusieurs cartes Pokémon) via un LLM Vision.
/// Implémenté dans Infrastructure (Ollama Vision). Dégradation gracieuse : en cas d'échec,
/// ne lève pas d'exception mais retourne un <see cref="CardVisionAnalysis"/> dont le
/// <see cref="CardVisionAnalysis.Status"/> décrit précisément la cause.
/// </summary>
public interface ICardVisionAnalyzer
{
    /// <param name="modelOverride">
    /// Modèle Ollama à utiliser pour cet appel (ex. "llava:7b", "llava-phi3", "moondream") — voir
    /// VisionModelSettingsReader (Paramètres › Scan, IA Vision). Null = utilise le modèle par
    /// défaut de la configuration statique (OllamaVisionOptions.Model), comportement historique
    /// conservé pour tout appelant qui n'a pas encore été migré vers le réglage dynamique.
    /// </param>
    Task<CardVisionAnalysis> AnalyzeAsync(
        string imageBase64,
        string ragContext,
        IReadOnlyList<string>? exclusions = null,
        string? modelOverride = null,
        CancellationToken ct = default);
}

/// <summary>Issue technique de l'appel au modèle Vision (indépendante du matching en base).</summary>
public enum CardVisionStatus
{
    /// <summary>Le modèle a répondu et sa réponse a été parsée (0 carte ou plus).</summary>
    Ok,
    /// <summary>Service injoignable, connexion refusée ou timeout.</summary>
    ServiceUnavailable,
    /// <summary>Le modèle demandé n'existe pas côté Ollama (nécessite `ollama pull`).</summary>
    ModelNotFound,
    /// <summary>Réponse HTTP OK mais corps illisible / JSON invalide / champ manquant.</summary>
    InvalidResponse
}

/// <summary>Résultat complet de l'analyse Vision : détections + statut technique + diagnostic.</summary>
public sealed record CardVisionAnalysis(
    IReadOnlyList<CardVisionResult> Results,
    CardVisionStatus Status,
    string? Diagnostic = null)
{
    public static CardVisionAnalysis Success(IReadOnlyList<CardVisionResult> results)
        => new(results, CardVisionStatus.Ok);

    public static CardVisionAnalysis Failure(CardVisionStatus status, string? diagnostic = null)
        => new(Array.Empty<CardVisionResult>(), status, diagnostic);
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
