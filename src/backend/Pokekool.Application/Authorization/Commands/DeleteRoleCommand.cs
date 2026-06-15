using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Authorization.Commands;

public sealed record DeleteRoleCommand(Guid Id) : IRequest;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionResolver _resolver;
    private readonly IUnitOfWork _uow;

    public DeleteRoleCommandHandler(IRoleRepository roles, IPermissionResolver resolver, IUnitOfWork uow)
    {
        _roles = roles;
        _resolver = resolver;
        _uow = uow;
    }

    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(request.Id, cancellationToken)
            ?? throw AuthorizationErrors.RoleNotFound(request.Id);

        if (role.IsSystem)
            throw AuthorizationErrors.SystemRoleImmutable();

        _roles.Remove(role);
        await _uow.SaveChangesAsync(cancellationToken);

        await _resolver.InvalidateRoleCacheAsync(role.Id, cancellationToken);
    }
}
