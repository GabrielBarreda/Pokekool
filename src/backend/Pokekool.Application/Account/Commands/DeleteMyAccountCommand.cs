using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Account.Commands;

/// <summary>Suppression (soft-delete) du compte du client authentifié lui-même.</summary>
public sealed record DeleteMyAccountCommand : IRequest;

public sealed class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public DeleteMyAccountCommandHandler(IUserRepository users, ICurrentUserService currentUser, IUnitOfWork uow)
    {
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessException("User not authenticated.");

        var user = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new DomainException("account.not_found", "User not found.");

        user.SoftDelete();
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
