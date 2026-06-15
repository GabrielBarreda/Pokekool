using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pokekool.Application.Abstractions;
using SharedClaimTypes = Pokekool.Shared.Constants.ClaimTypes;

namespace Pokekool.Infrastructure.Services;

internal sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var raw = _accessor.HttpContext?.User?.FindFirst(SharedClaimTypes.UserId)?.Value
                   ?? _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => _accessor.HttpContext?.User?.FindFirst(SharedClaimTypes.Email)?.Value
                         ?? _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        _accessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? new List<string>();
}
