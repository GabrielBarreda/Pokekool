using FluentValidation;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Authorization;

namespace Pokekool.Application.Authorization.Commands;

public sealed record UpdateRoleCommand(Guid Id, UpdateRoleRequest Request) : IRequest<RoleDto>;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Request.Name).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Request.Description).MaximumLength(500);
    }
}

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IPermissionResolver _resolver;
    private readonly IUnitOfWork _uow;

    public UpdateRoleCommandHandler(
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

    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(request.Id, cancellationToken)
            ?? throw AuthorizationErrors.RoleNotFound(request.Id);

        if (role.IsSystem)
            throw AuthorizationErrors.SystemRoleImmutable();

        // Validation existence + escalation
        var requested = request.Request.Permissions.Distinct().ToList();
        var catalog = (await _permissions.ListAllAsync(cancellationToken)).Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var bad = requested.FirstOrDefault(c => !catalog.Contains(c));
        if (bad is not null) throw AuthorizationErrors.InvalidPermissionCode(bad);

        var mine = await _resolver.GetPermissionsAsync(cancellationToken);
        var missing = requested.Where(c => !mine.Contains(c)).ToList();
        if (missing.Count > 0) throw AuthorizationErrors.CannotEscalatePrivileges(missing);

        role.Rename(request.Request.Name);
        role.UpdateDescription(request.Request.Description);
        role.ReplacePermissions(requested);

        await _uow.SaveChangesAsync(cancellationToken);

        // Invalide le cache Redis pour ce rôle (les permissions ont changé)
        await _resolver.InvalidateRoleCacheAsync(role.Id, cancellationToken);

        return role.ToDto();
    }
}
