namespace Pokekool.Mobile.Services;

/// <summary>
/// Ajoute le header X-Display-Language sur chaque requête API, lu par
/// ICurrentDisplayLanguageService côté backend pour résoudre DisplayName (voir ADR-004,
/// mémoire Obsidian du projet). Miroir de AuthHttpMessageHandler.
/// </summary>
public sealed class DisplayLanguageHttpMessageHandler : DelegatingHandler
{
    private readonly CardDisplayLanguageService _displayLanguage;

    public DisplayLanguageHttpMessageHandler(CardDisplayLanguageService displayLanguage)
        => _displayLanguage = displayLanguage;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Remove("X-Display-Language");
        request.Headers.TryAddWithoutValidation("X-Display-Language", _displayLanguage.Language);
        return base.SendAsync(request, ct);
    }
}
