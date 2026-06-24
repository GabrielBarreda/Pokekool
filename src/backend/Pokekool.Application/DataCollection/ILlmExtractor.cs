namespace Pokekool.Application.DataCollection;

/// <summary>
/// Abstraction d'un extracteur LLM : envoie du HTML nettoyé et retourne du JSON structuré.
/// </summary>
public interface ILlmExtractor
{
    /// <summary>
    /// Envoie le HTML nettoyé au LLM avec un prompt d'extraction.
    /// </summary>
    /// <param name="htmlContent">Contenu HTML nettoyé (tronqué).</param>
    /// <param name="jsonSchema">Schéma JSON attendu en sortie.</param>
    /// <param name="ct">Token d'annulation.</param>
    /// <returns>JSON brut (string) ou null si échec/LLM indisponible.</returns>
    Task<string?> ExtractJsonAsync(string htmlContent, string jsonSchema, CancellationToken ct = default);
}
