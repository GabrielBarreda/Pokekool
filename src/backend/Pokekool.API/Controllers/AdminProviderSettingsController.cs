using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pokekool.API.Authorization;
using Pokekool.Application.Settings.Commands.UpsertProviderSetting;
using Pokekool.Application.Settings.Queries.GetProviderSettings;
using Pokekool.Infrastructure.DataProviders.CardMarket;  // ICardMarketClient (interface publique)
using Pokekool.Shared.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace Pokekool.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/providers")]
[Produces("application/json")]
[Authorize]
public sealed class AdminProviderSettingsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICardMarketClient _cardMarketClient;

    public AdminProviderSettingsController(IMediator mediator, ICardMarketClient cardMarketClient)
    {
        _mediator = mediator;
        _cardMarketClient = cardMarketClient;
    }

    /// <summary>
    /// Retourne les clés configurées pour un provider (valeurs masquées).
    /// </summary>
    [HttpGet("{provider}/settings")]
    [RequirePermission(Permissions.SettingsRead)]
    [ProducesResponseType(typeof(ProviderSettingsDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Get provider settings (keys only, values masked)")]
    public async Task<ActionResult<ProviderSettingsDto>> GetSettings(
        string provider, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProviderSettingsQuery(provider), ct);
        return Ok(result);
    }

    /// <summary>
    /// Chiffre et persiste une valeur de configuration pour un provider.
    /// La valeur en clair n'est jamais stockée — uniquement sa version chiffrée.
    /// </summary>
    [HttpPut("{provider}/settings/{key}")]
    [RequirePermission(Permissions.SettingsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Upsert a provider setting (value will be encrypted)")]
    public async Task<IActionResult> UpsertSetting(
        string provider,
        string key,
        [FromBody] UpsertSettingRequest body,
        CancellationToken ct = default)
    {
        await _mediator.Send(new UpsertProviderSettingCommand(provider, key, body.Value), ct);
        return NoContent();
    }

    /// <summary>
    /// Teste la connexion au provider CardMarket avec les credentials configurés.
    /// </summary>
    [HttpPost("cardmarket/test-connection")]
    [RequirePermission(Permissions.SettingsRead)]
    [ProducesResponseType(typeof(TestConnectionResultDto), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Test CardMarket connection with configured credentials")]
    public async Task<ActionResult<TestConnectionResultDto>> TestCardMarketConnection(
        CancellationToken ct = default)
    {
        var ok = await _cardMarketClient.TestConnectionAsync(ct);
        return Ok(new TestConnectionResultDto(ok, ok ? "Connexion réussie" : "Credentials invalides ou manquants"));
    }
}

public sealed record UpsertSettingRequest(string Value);

public sealed record TestConnectionResultDto(bool Success, string Message);
