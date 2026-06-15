using FluentValidation;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Authorization;
using Pokekool.Domain.Entities;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.ValueObjects;
using Pokekool.Shared.Constants;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Commands;

public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<UserDto>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.Request.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(c => c.Request.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(c => c.Request.FirstName).MaximumLength(120);
        RuleFor(c => c.Request.LastName).MaximumLength(120);
        RuleFor(c => c.Request.Phone).MaximumLength(30);
        RuleFor(c => c.Request.RoleIds).NotNull();
    }
}

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _hasher;
    private readonly IPermissionResolver _resolver;
    private readonly IUnitOfWork _uow;

    public CreateUserCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher hasher,
        IPermissionResolver resolver,
        IUnitOfWork uow)
    {
        _users = users;
        _roles = roles;
        _hasher = hasher;
        _resolver = resolver;
        _uow = uow;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;

        var email = Email.Create(r.Email);
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null) throw UserErrors.EmailAlreadyTaken(r.Email);

        var roles = await _roles.GetManyByIdsAsync(r.RoleIds, cancellationToken);
        if (roles.Count != r.RoleIds.Distinct().Count())
        {
            var missing = r.RoleIds.Except(roles.Select(rr => rr.Id)).First();
            throw UserErrors.RoleNotFound(missing);
        }
        if (roles.Any(rr => rr.Code == SystemRoles.SuperAdmin))
            throw UserErrors.CannotAssignSuperAdmin();

        var mine = await _resolver.GetPermissionsAsync(cancellationToken);
        var requestedPerms = roles.SelectMany(rr => rr.PermissionCodes).Distinct();
        var missingPerms = requestedPerms.Where(p => !mine.Contains(p)).ToList();
        if (missingPerms.Count > 0)
            throw AuthorizationErrors.CannotEscalatePrivileges(missingPerms);

        var user = User.Create(email, _hasher.Hash(r.Password), r.FirstName, r.LastName);
        user.UpdateProfile(r.FirstName, r.LastName, r.Phone);
        foreach (var role in roles) user.AssignRole(role.Id);

        await _users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var rolesById = roles.ToDictionary(rr => rr.Id);
        return user.ToDto(rolesById);
    }
}
