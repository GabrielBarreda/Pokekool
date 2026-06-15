using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Application.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, LoginResponse>
{
    private readonly IRefreshTokenStore _refreshStore;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IJwtTokenGenerator _jwt;

    public RefreshCommandHandler(
        IRefreshTokenStore refreshStore,
        IUserRepository users,
        IRoleRepository roles,
        IJwtTokenGenerator jwt)
    {
        _refreshStore = refreshStore;
        _users = users;
        _roles = roles;
        _jwt = jwt;
    }

    public async Task<LoginResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var data = await _refreshStore.GetAsync(request.RefreshToken, cancellationToken);
        if (data is null || data.ExpiresAt < DateTimeOffset.UtcNow)
            throw Auth.AuthErrors.InvalidRefreshToken();

        var user = await _users.GetByIdAsync(data.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw Auth.AuthErrors.InvalidRefreshToken();

        // Rotation : on révoque l'ancien et on en émet un nouveau.
        await _refreshStore.RevokeAsync(request.RefreshToken, cancellationToken);

        var roleIds = user.RoleAssignments.Select(ra => ra.RoleId).Distinct().ToList();
        var roles = await _roles.GetManyByIdsAsync(roleIds, cancellationToken);
        var roleCodes = roles.Select(r => r.Code).Distinct().ToList();

        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user, roleCodes);
        var newRefresh = _jwt.GenerateRefreshToken();

        await _refreshStore.SaveAsync(newRefresh, user.Id, user.TenantId, TimeSpan.FromDays(7), cancellationToken);

        var summary = new UserSummaryDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.TenantId,
            roleCodes);

        return new LoginResponse(accessToken, newRefresh, expiresAt, summary);
    }
}
