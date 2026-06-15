using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Users.Commands;

public sealed record SetUserActivationCommand(Guid UserId, bool IsActive) : IRequest;

public sealed class SetUserActivationCommandHandler : IRequestHandler<SetUserActivationCommand>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public SetUserActivationCommandHandler(
        IUserRepository users,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(SetUserActivationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == request.UserId)
            throw UserErrors.CannotActOnSelf();

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw UserErrors.UserNotFound(request.UserId);

        if (!request.IsActive)
            user.Deactivate();
        else
            user.Activate();

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
