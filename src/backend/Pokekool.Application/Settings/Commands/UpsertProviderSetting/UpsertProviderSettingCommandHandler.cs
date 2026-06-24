using MediatR;
using Pokekool.Application.Abstractions;

namespace Pokekool.Application.Settings.Commands.UpsertProviderSetting;

internal sealed class UpsertProviderSettingCommandHandler
    : IRequestHandler<UpsertProviderSettingCommand, Unit>
{
    private readonly IProviderSettingsService _settings;

    public UpsertProviderSettingCommandHandler(IProviderSettingsService settings)
        => _settings = settings;

    public async Task<Unit> Handle(UpsertProviderSettingCommand request, CancellationToken ct)
    {
        await _settings.SetAsync(request.Provider, request.Key, request.PlainValue, ct);
        return Unit.Value;
    }
}
