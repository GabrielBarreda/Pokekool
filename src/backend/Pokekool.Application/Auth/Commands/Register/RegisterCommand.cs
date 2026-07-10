using FluentValidation;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.ValueObjects;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Application.Auth.Commands.Register;

/// <summary>
/// Inscription publique d'un compte client. Assigne le rôle système "client"
/// puis connecte immédiatement l'utilisateur (retourne un LoginResponse).
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName) : IRequest<LoginResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(c => c.FirstName).MaximumLength(100);
        RuleFor(c => c.LastName).MaximumLength(100);
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, LoginResponse>
{
    public const string ClientRoleCode = "client";

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenStore _refreshStore;
    private readonly IUnitOfWork _uow;

    public RegisterCommandHandler(
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

    public async Task<LoginResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            throw new DomainException("auth.email_taken", "An account with this email already exists.");

        var user = User.Create(
            email,
            _hasher.Hash(request.Password),
            request.FirstName,
            request.LastName);

        // Rôle système "client" — doit être seedé en base. On l'assigne s'il existe.
        var clientRole = await _roles.GetByCodeAsync(tenantId: null, ClientRoleCode, cancellationToken);
        if (clientRole is not null)
            user.AssignRole(clientRole.Id);

        await _users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var roleCodes = clientRole is not null
            ? new List<string> { clientRole.Code }
            : new List<string>();

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
