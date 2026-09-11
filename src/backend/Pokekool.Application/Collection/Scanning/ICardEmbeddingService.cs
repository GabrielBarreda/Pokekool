namespace Pokekool.Application.Collection.Scanning;

/// <summary>
/// Calcule l'embedding visuel d'une image de carte via un service DINOv2 (Meta AI, licence
/// Apache 2.0 — voir services/card-embedding/). Implémenté dans Infrastructure. Même contrat
/// de dégradation gracieuse que <see cref="ICardVisionAnalyzer"/> : jamais d'exception, un
/// statut explicite à la place.
/// </summary>
public interface ICardEmbeddingService
{
    /// <summary>Encode une image (photo de scan) déjà en base64.</summary>
    Task<CardEmbeddingResult> EmbedAsync(string imageBase64, CancellationToken ct = default);

    /// <summary>Télécharge l'image à l'URL donnée (ex: image officielle PokemonTCG.io) puis
    /// l'encode — utilisé par le job d'indexation du catalogue.</summary>
    Task<CardEmbeddingResult> EmbedFromUrlAsync(string imageUrl, CancellationToken ct = default);
}

/// <summary>Issue technique de l'appel au service d'embedding (indépendante du matching en base).</summary>
public enum CardEmbeddingStatus
{
    /// <summary>Le service a répondu avec un vecteur exploitable.</summary>
    Ok,
    /// <summary>Service injoignable, connexion refusée ou timeout.</summary>
    ServiceUnavailable,
    /// <summary>Réponse HTTP OK mais corps illisible / vecteur vide.</summary>
    InvalidResponse
}

/// <summary>Résultat de l'encodage : vecteur + statut technique + diagnostic.</summary>
public sealed record CardEmbeddingResult(float[]? Embedding, CardEmbeddingStatus Status, string? Diagnostic = null)
{
    public static CardEmbeddingResult Success(float[] embedding) => new(embedding, CardEmbeddingStatus.Ok);

    public static CardEmbeddingResult Failure(CardEmbeddingStatus status, string? diagnostic = null)
        => new(null, status, diagnostic);
}
