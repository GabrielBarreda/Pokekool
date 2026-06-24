using MediatR;

namespace Pokekool.Application.Settings.Commands.UpsertProviderSetting;

/// <summary>
/// Chiffre et persiste une clé de configuration pour un provider.
/// Ex : provider = "cardmarket", key = "app_token", value = "abc123..."
/// </summary>
public sealed record UpsertProviderSettingCommand(
    string Provider,
    string Key,
    string PlainValue)
    : IRequest<Unit>;
