using System.Net;
using System.Net.Http.Headers;

namespace Pokekool.Mobile.Services;

/// <summary>
/// Injecte le Bearer token et retente une fois sur 401 après refresh
/// (miroir de l'intercepteur HTTP Angular).
/// </summary>
public sealed class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly SessionService _session;
    private readonly AuthService _auth;

    public AuthHttpMessageHandler(SessionService session, AuthService auth)
    {
        _session = session;
        _auth = auth;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_session.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // 401 → tentative de refresh puis rejeu unique de la requête.
        if (!await _auth.TryRefreshAsync(ct))
            return response;

        response.Dispose();
        var retry = await CloneRequestAsync(request, ct);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        return await base.SendAsync(retry, ct);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
        };

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(ct);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
