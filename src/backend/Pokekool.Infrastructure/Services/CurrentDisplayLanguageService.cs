using Microsoft.AspNetCore.Http;
using Pokekool.Application.Abstractions;

namespace Pokekool.Infrastructure.Services;

internal sealed class CurrentDisplayLanguageService : ICurrentDisplayLanguageService
{
    public const string HeaderName = "X-Display-Language";
    private const string DefaultLanguage = "en";

    private readonly IHttpContextAccessor _accessor;

    public CurrentDisplayLanguageService(IHttpContextAccessor accessor) => _accessor = accessor;

    public string Language
    {
        get
        {
            var raw = _accessor.HttpContext?.Request?.Headers[HeaderName].ToString();
            return string.IsNullOrWhiteSpace(raw) ? DefaultLanguage : raw.Trim().ToLowerInvariant();
        }
    }
}
