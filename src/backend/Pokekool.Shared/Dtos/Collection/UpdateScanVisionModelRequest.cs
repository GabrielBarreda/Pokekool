namespace Pokekool.Shared.Dtos.Collection;

/// <summary>Requête de changement du modèle Ollama Vision (Paramètres › Scan, IA Vision) — Model
/// doit être l'une des valeurs de ScanVisionModelSettingsDto.AllowedModels (ex. "llava:7b",
/// "llava-phi3", "moondream"), sinon rejeté par UpdateScanVisionModelSettingsCommandHandler.</summary>
public sealed record UpdateScanVisionModelRequest(string Model);
