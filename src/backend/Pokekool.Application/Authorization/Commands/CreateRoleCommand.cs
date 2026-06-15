using FluentValidation;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Entities.Authorization;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Authorization;

namespace Pokekool.Application.Authorization.Commands;

public sealed record CreateRoleCommand(CreateRoleRequest Request) : IRequest<RoleDto>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(c => c.Request.Code).NotEmpty().MaximumLength(40)
            .Matches(@"^[a-z0-9_]+$").WithMessage("Code must be lowercase, digits and underscores only.");
        RuleFor(c => c.Request.Name).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Request.Description).MaximumLength(500);
    }
}

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IPermissionResolver _resolver;
    private readonly IUnitOfWork _uow;

    public CreateRoleCommandHandler(
        IRoleRepository roles,
        IPermissionRepository permissions,
        IPermissionResolver resolver,
        IUnitOfWork uow)
    {
        _roles = roles;
        _permissions = permissions;
        _resolver = resolver;
        _uow = uow;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;

        // Unicité du code parmi les rôles système.
        var existing = await _roles.GetByCodeAsync(null, r.Code, cancellationToken);
        if (existing is not null)
            throw AuthorizationErrors.RoleCodeAlreadyExists(r.Code);

        var requested = (r.Permissions ?? Array.Empty<string>()).Distinct().ToList();
        await EnsurePermissionsValidAsync(requested, cancellationToken);
        await EnsureNoEscalationAsync(requested, cancellationToken);

        // Custom roles have no tenantId in Pokekool
        var role = Role.CreateSystemRole(r.Code, r.Name, r.Description);
        role.SeedPermissions(requested);

        await _roles.AddAsync(role, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return role.ToDto();
    }

    private async Task EnsurePermissionsValidAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        var catalog = (await _permissions.ListAllAsync(cancellationToken)).Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var bad = codes.FirstOrDefault(c => !catalog.Contains(c));
        if (bad is not null) throw AuthorizationErrors.InvalidPermissionCode(bad);
    }

    private async Task EnsureNoEscalationAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        var mine = await _resolver.GetPermissionsAsync(cancellationToken);
        var missing = codes.Where(c => !mine.Contains(c)).ToList();
        if (missing.Count > 0) throw AuthorizationErrors.CannotEscalatePrivileges(missing);
    }
}
