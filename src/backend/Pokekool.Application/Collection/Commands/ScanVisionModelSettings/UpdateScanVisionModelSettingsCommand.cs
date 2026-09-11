using MediatR;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Collection.Commands.ScanVisionModelSettings;

/// <summary>Change le modèle Ollama Vision utilisé par le fallback texte/vision (upsert dans
/// AppSetting) — Paramètres › Scan (IA Vision).</summary>
public sealed record UpdateScanVisionModelSettingsCommand(string Model) : IRequest;

internal sealed class UpdateScanVisionModelSettingsCommandHandler
    : IRequestHandler<UpdateScanVisionModelSettingsCommand>
{
    private readonly IAppSettingRepository _settings;
    private readonly IUnitOfWork _uow;

    public UpdateScanVisionModelSettingsCommandHandler(IAppSettingRepository settings, IUnitOfWork uow)
    {
        _settings = settings;
        _uow = uow;
    }

    public async Task Handle(UpdateScanVisionModelSettingsCommand request, CancellationToken ct)
    {
        if (!VisionModelSettingsKeys.AllowedModels.Contains(request.Model))
            throw new DomainException(
                "scan_vision_model.invalid",
                $"Modèle '{request.Model}' non autorisé. Valeurs possibles : {string.Join(", ", VisionModelSettingsKeys.AllowedModels)}.");

        var existing = await _settings.GetByKeyAsync(VisionModelSettingsKeys.Model, ct);
        if (existing is null)
            await _settings.AddAsync(AppSetting.Create(VisionModelSettingsKeys.Model, request.Model), ct);
        else
            existing.SetValue(request.Model);

        await _uow.SaveChangesAsync(ct);
    }
}
