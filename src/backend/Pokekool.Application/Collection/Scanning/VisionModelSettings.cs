using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Collection.Scanning;

/// <summary>
/// Modèle Ollama Vision utilisé pour le fallback texte/vision du scan (llava/OCR) — réglage
/// modifiable depuis Paramètres › Scan (IA Vision), persisté via AppSetting (voir
/// StorageSettingsKeys/StorageSettingsReader pour le même pattern côté module Stockage). Trois
/// modèles proposés, du plus lourd/précis au plus léger — comparatif CPU demandé le 2026-09-11
/// suite à un pic à 569% observé avec llava:7b sur un run boutique réel :
/// - llava:7b (défaut historique — le plus lourd, référence qualité)
/// - llava-phi3 (~3.8B, backbone Phi-3-mini — garde l'architecture "vision + raisonnement"
///   de llava, donc a priori le meilleur compromis pour lire une étiquette de gradation
///   PSA/BGS/CGC en plus du texte de carte, tout en étant nettement plus léger)
/// - moondream (~1.8B, le plus léger — optimisé description/VQA courte, moins fiable a priori
///   sur du texte dense/petit type étiquette de gradation)
/// </summary>
public static class VisionModelSettingsKeys
{
    public const string Model = "scan.vision_model";
    public const string ModelDefault = "llava:7b";

    /// <summary>Modèles proposés au choix dans Paramètres › Scan (IA Vision) — toute autre
    /// valeur stockée en base (ex. réglage legacy) est ignorée au profit du défaut.</summary>
    public static readonly IReadOnlyList<string> AllowedModels = new[] { "llava:7b", "llava-phi3", "moondream" };
}

/// <summary>Helper partagé : lecture typée du modèle Vision choisi (réutilisé par les handlers
/// qui appellent ICardVisionAnalyzer.AnalyzeAsync).</summary>
public static class VisionModelSettingsReader
{
    public static async Task<string> GetModelAsync(IAppSettingRepository settings, CancellationToken ct)
    {
        var entry = await settings.GetByKeyAsync(VisionModelSettingsKeys.Model, ct);
        if (entry is null || string.IsNullOrWhiteSpace(entry.Value))
            return VisionModelSettingsKeys.ModelDefault;

        return VisionModelSettingsKeys.AllowedModels.Contains(entry.Value)
            ? entry.Value
            : VisionModelSettingsKeys.ModelDefault;
    }
}
