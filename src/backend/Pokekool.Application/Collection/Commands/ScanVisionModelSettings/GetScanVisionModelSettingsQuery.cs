using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Application.Collection.Commands.ScanVisionModelSettings;

/// <summary>Lit le modèle Ollama Vision actuellement sélectionné (défaut si absent en base) —
/// voir VisionModelSettingsKeys/VisionModelSettingsReader.</summary>
public sealed record GetScanVisionModelSettingsQuery : IRequest<ScanVisionModelSettingsDto>;

internal sealed class GetScanVisionModelSettingsQueryHandler
    : IRequestHandler<GetScanVisionModelSettingsQuery, ScanVisionModelSettingsDto>
{
    private readonly IAppSettingRepository _settings;

    public GetScanVisionModelSettingsQueryHandler(IAppSettingRepository settings) => _settings = settings;

    public async Task<ScanVisionModelSettingsDto> Handle(
        GetScanVisionModelSettingsQuery request, CancellationToken ct)
    {
        var model = await Pokekool.Application.Collection.Scanning.VisionModelSettingsReader.GetModelAsync(_settings, ct);
        return new ScanVisionModelSettingsDto(
            model, Pokekool.Application.Collection.Scanning.VisionModelSettingsKeys.AllowedModels);
    }
}
