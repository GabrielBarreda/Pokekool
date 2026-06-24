using MediatR;

namespace Pokekool.Application.Settings.Queries.GetProviderSettings;

/// <summary>
/// Retourne les clés configurées pour un provider (valeurs masquées, jamais en clair).
/// </summary>
public sealed record GetProviderSettingsQuery(string Provider)
    : IRequest<ProviderSettingsDto>;

public sealed record ProviderSettingsDto(
    string Provider,
    IReadOnlyList<ProviderSettingKeyDto> Keys);

/// <summary>
/// IsConfigured = true si la clé existe en base, false sinon.
/// Value est fourni uniquement pour les clés non-sensibles (ex: extraction_mode).
/// Les secrets ne retournent jamais leur valeur.
/// </summary>
public sealed record ProviderSettingKeyDto(string Key, bool IsConfigured, string? Value = null);
