using FluentValidation;
using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Commands;

public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IRequest<UserDto>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Request.FirstName).MaximumLength(120);
        RuleFor(c => c.Request.LastName).MaximumLength(120);
        RuleFor(c => c.Request.Phone).MaximumLength(30);
    }
}

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _uow;

    public UpdateUserCommandHandler(IUserRepository users, IRoleRepository roles, IUnitOfWork uow)
    {
        _users = users;
        _roles = roles;
        _uow = uow;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.Id, cancellationToken)
            ?? throw UserErrors.UserNotFound(request.Id);

        user.UpdateProfile(request.Request.FirstName, request.Request.LastName, request.Request.Phone);
        await _uow.SaveChangesAsync(cancellationToken);

        var roleIds = user.RoleAssignments.Select(ra => ra.RoleId).Distinct().ToList();
        var roles = (await _roles.GetManyByIdsAsync(roleIds, cancellationToken)).ToDictionary(r => r.Id);
        return user.ToDto(roles);
    }
}
