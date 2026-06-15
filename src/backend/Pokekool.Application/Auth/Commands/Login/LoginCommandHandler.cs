using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.ValueObjects;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenStore _refreshStore;
    private readonly IUnitOfWork _uow;

    public LoginCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt,
        IRefreshTokenStore refreshStore,
        IUnitOfWork uow)
    {
        _users = users;
        _roles = roles;
        _hasher = hasher;
        _jwt = jwt;
        _refreshStore = refreshStore;
        _uow = uow;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw AuthErrors.InvalidCredentials();

        if (!user.IsActive)
            throw AuthErrors.AccountDisabled();

        var roleIds = user.RoleAssignments.Select(ra => ra.RoleId).Distinct().ToList();
        var roles = await _roles.GetManyByIdsAsync(roleIds, cancellationToken);
        var roleCodes = roles.Select(r => r.Code).Distinct().ToList();

        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user, roleCodes);
        var refreshToken = _jwt.GenerateRefreshToken();

        await _refreshStore.SaveAsync(
            refreshToken,
            user.Id,
            user.TenantId,
            TimeSpan.FromDays(7),
            cancellationToken);

        user.RecordLogin();
        await _uow.SaveChangesAsync(cancellationToken);

        var summary = new UserSummaryDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.TenantId,
            roleCodes);

        return new LoginResponse(accessToken, refreshToken, expiresAt, summary);
    }
}
