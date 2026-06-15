using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Users.Commands;

public sealed record DeleteUserCommand(Guid UserId) : IRequest;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public DeleteUserCommandHandler(
        IUserRepository users,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == request.UserId)
            throw UserErrors.CannotActOnSelf();

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw UserErrors.UserNotFound(request.UserId);

        user.SoftDelete();
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
