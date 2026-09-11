using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pokekool.Mobile.Services;

/// <summary>
/// Client HTTP typé, base <c>api/v1</c>. Toutes les routes des services métier sont relatives
/// (ex. <c>pokemon/series</c>) — pas de duplication du préfixe.
/// </summary>
public sealed class ApiClient
{
    public const string HttpClientName = "PokekoolApi";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        // Le backend sérialise les enums en texte (JsonStringEnumConverter dans Program.cs) —
        // sans ce convertisseur ici, la désérialisation d'un champ enum (ex. ScanStatus) plante
        // avec "The JSON value could not be converted...".
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IHttpClientFactory _factory;

    public ApiClient(IHttpClientFactory factory) => _factory = factory;

    private HttpClient Client => _factory.CreateClient(HttpClientName);

    public async Task<T> GetAsync<T>(string route, CancellationToken ct = default)
    {
        var response = await Client.GetAsync(ApiConfig.V1 + route, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
    }

    public async Task<TResult> PostAsync<TResult>(string route, object? body = null, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync(ApiConfig.V1 + route, body, Json, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<TResult>(Json, ct))!;
    }

    public async Task PostAsync(string route, object? body = null, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync(ApiConfig.V1 + route, body, Json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<TResult> PostMultipartAsync<TResult>(string route, MultipartFormDataContent content, CancellationToken ct = default)
    {
        var response = await Client.PostAsync(ApiConfig.V1 + route, content, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<TResult>(Json, ct))!;
    }

    public async Task<TResult> PutAsync<TResult>(string route, object? body = null, CancellationToken ct = default)
    {
        var response = await Client.PutAsJsonAsync(ApiConfig.V1 + route, body, Json, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<TResult>(Json, ct))!;
    }

    public async Task PutAsync(string route, object? body = null, CancellationToken ct = default)
    {
        var response = await Client.PutAsJsonAsync(ApiConfig.V1 + route, body, Json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<TResult> PatchAsync<TResult>(string route, object? body = null, CancellationToken ct = default)
    {
        var response = await Client.PatchAsJsonAsync(ApiConfig.V1 + route, body, Json, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<TResult>(Json, ct))!;
    }

    public async Task DeleteAsync(string route, CancellationToken ct = default)
    {
        var response = await Client.DeleteAsync(ApiConfig.V1 + route, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        string detail;
        try
        {
            detail = await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            detail = string.Empty;
        }

        throw new ApiException(response.StatusCode, detail);
    }
}

public sealed class ApiException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public string Detail { get; }

    public ApiException(System.Net.HttpStatusCode statusCode, string detail)
        : base($"API {(int)statusCode}: {Truncate(detail)}")
    {
        StatusCode = statusCode;
        Detail = detail;
    }

    private static string Truncate(string s) => s.Length > 300 ? s[..300] + "…" : s;
}
